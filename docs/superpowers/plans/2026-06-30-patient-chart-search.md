# Patient Chart Search Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the dashboard's "list every patient" Patient Chart entry with a single-menu, search-first page (name/code + provider + clinic + status filters) that opens a patient's chart, and reach demographics editing from the chart.

**Architecture:** Backend adds two optional filters (`ProviderId`, `ClinicId`) to the existing `SearchPatients` slice, filtered in-module via an `EXISTS` over `PatientReports` (which carry `PatientId`/`ProviderId`/`ClinicId`). Frontend rewrites `patient-charts/list.tsx` into a search-first page, removes the standalone `Patients` menu item + `/patients` list page, extracts the create-patient dialog into a shared component, and repoints the demographics detail page's navigation at the chart hub.

**Tech Stack:** .NET 10 / EF Core 10 / Mediator (backend); React 19 + Vite + TanStack Query v5 + React Router 7 + Tailwind (dashboard); Playwright (route-mocked E2E).

## Global Constraints

- Backend handlers: `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)` on every await, propagate `CancellationToken`. (AGENTS.md golden rules 5, 7)
- No `src/BuildingBlocks` changes. No cross-module references (Patient stays in-module). (golden rules 1, 4)
- Build runs with `TreatWarningsAsErrors` — warnings fail the build. File-scoped namespaces, explicit types, `is null`/`is not null`.
- No database migration (no schema change).
- Frontend: pass per-call data through `mutate(arg)` / query params, never via closed-over state. (golden rule 9)
- Docs + changelog travel with the change (golden rule 10) — see Task 9.
- Spec: `docs/superpowers/specs/2026-06-30-patient-chart-search-design.md`.

---

### Task 1: Backend — provider/clinic filters on SearchPatients

**Files:**
- Modify: `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/SearchPatientsQuery.cs`
- Modify: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/SearchPatients/SearchPatientsQueryHandler.cs`
- Modify: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/SearchPatients/SearchPatientsEndpoint.cs`

**Interfaces:**
- Produces: `SearchPatientsQuery(... Guid? ProviderId = null, Guid? ClinicId = null)`; GET `/api/v1/patient/patients` now also accepts `providerId` and `clinicId` query-string GUIDs.

- [ ] **Step 1: Add the two optional parameters to the query record**

In `SearchPatientsQuery.cs`, replace the record with:

```csharp
public sealed record SearchPatientsQuery(
    string? Search = null,
    string? SsnHash = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null,
    Guid? ProviderId = null,
    Guid? ClinicId = null) : IQuery<PagedResponse<PatientListItemDto>>;
```

- [ ] **Step 2: Add the EXISTS filters in the handler**

In `SearchPatientsQueryHandler.cs`, immediately after the `IsActive` block (the
`if (query.IsActive.HasValue) { ... }`) and before `q = ApplySort(...)`, insert:

```csharp
        if (query.ProviderId is { } providerId)
        {
            q = q.Where(p => dbContext.PatientReports.Any(r =>
                r.PatientId == p.Id && !r.IsDeleted && r.ProviderId == providerId));
        }

        if (query.ClinicId is { } clinicId)
        {
            q = q.Where(p => dbContext.PatientReports.Any(r =>
                r.PatientId == p.Id && !r.IsDeleted && r.ClinicId == clinicId));
        }
```

(`dbContext.PatientReports` is the existing `DbSet`; `PatientReport` exposes
`PatientId`, `ProviderId`, `ClinicId`, `IsDeleted` — confirmed in
`Domain/PatientReport.cs`.)

- [ ] **Step 3: Bind the new query-string params in the endpoint**

In `SearchPatientsEndpoint.cs`, add the two parameters to the delegate (after
`sortDir`, before `IMediator mediator`) and pass them into the query:

```csharp
        return endpoints.MapGet("/patients",
                async (
                    string? search,
                    string? ssnHash,
                    bool? isActive,
                    int pageNumber,
                    int pageSize,
                    string? sortBy,
                    string? sortDir,
                    Guid? providerId,
                    Guid? clinicId,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientsQuery(
                            search, ssnHash, isActive, pageNumber, pageSize,
                            sortBy, sortDir, providerId, clinicId), ct)))
            .WithName("SearchPatients")
            .WithSummary("Search and list patients (no PHI in response)")
            .RequirePermission(PatientPermissions.Patients.View);
```

- [ ] **Step 4: Create the missing SearchPatients validator (golden rule 8)**

`SearchPatientsQuery` is a paginated query but has NO validator today — this is a
pre-existing Architecture.Tests failure
(`HandlerValidatorPairingTests.QueryHandlers_With_Pagination_Should_Have_Validators`).
Since this task owns the slice, add it. Mirror
`SearchPatientIncidentsQueryValidator` (no required PatientId on this query, so
only the page-size bound). Create
`src/Modules/Patient/Modules.Patient/Features/v1/Patients/SearchPatients/SearchPatientsQueryValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.Patients;

namespace FSH.Modules.Patient.Features.v1.Patients.SearchPatients;

public sealed class SearchPatientsQueryValidator : AbstractValidator<SearchPatientsQuery>
{
    public SearchPatientsQueryValidator()
    {
        RuleFor(x => x.PageSize).LessThanOrEqualTo(200);
    }
}
```

- [ ] **Step 5: Build the backend**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: build succeeds with 0 warnings/0 errors (warnings-as-errors). The new
`EXISTS` translates to SQL; no migration required.

- [ ] **Step 6: Run the handler-validator pairing arch test**

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj --filter "FullyQualifiedName~HandlerValidatorPairingTests"`
Expected: PASS — the new validator clears the pre-existing
`SearchPatientsQueryHandler ... has no validator` failure. (Two OTHER pre-existing
arch failures remain until Tasks 10 and 11.)

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Patient
git commit -m "feat(patient): add provider/clinic filters to patient search

In-module EXISTS over PatientReports; no schema change. Adds the missing
SearchPatientsQueryValidator (golden rule 8 / Architecture.Tests).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Frontend API — thread providerId/clinicId through searchPatients

**Files:**
- Modify: `clients/dashboard/src/api/patients.ts:22-47`

**Interfaces:**
- Produces: `SearchPatientsParams` gains `providerId?: string` and `clinicId?: string`; `searchPatients` serializes them.

- [ ] **Step 1: Extend the params type**

In `SearchPatientsParams` (currently ending with `sortDir?: "asc" | "desc";`), add:

```typescript
  providerId?: string;
  clinicId?: string;
```

- [ ] **Step 2: Serialize the new params**

In `searchPatients`, after the `sortDir` line
(`if (params.sortDir) query.set("sortDir", params.sortDir);`) add:

```typescript
  if (params.providerId) query.set("providerId", params.providerId);
  if (params.clinicId) query.set("clinicId", params.clinicId);
```

- [ ] **Step 3: Typecheck**

Run: `cd clients/dashboard && npm run build`
Expected: `tsc -b` passes (no type errors), vite build completes.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/api/patients.ts
git commit -m "feat(dashboard): add providerId/clinicId to searchPatients params

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Extract the Register-a-patient dialog into a shared component

**Files:**
- Create: `clients/dashboard/src/pages/patients/create-patient-dialog.tsx`
- Modify: `clients/dashboard/src/pages/patients/list.tsx` (import the shared dialog instead of the inline one — temporary until the file is deleted in Task 7; this keeps `list.tsx` compiling and its Playwright test green in between)

**Interfaces:**
- Produces: `CreatePatientDialog({ open, onClose, onCreated? })` where
  `onCreated?: (patientId: string) => void` runs after a successful create
  (in addition to the toast + list invalidation). When omitted, behavior is
  exactly today's (toast, invalidate `["patients","list"]`, close).

- [ ] **Step 1: Create the shared dialog file**

Create `clients/dashboard/src/pages/patients/create-patient-dialog.tsx` by moving
the existing `CreatePatientDialog` function out of `list.tsx` verbatim, then making
two changes: export it, and add the optional `onCreated` callback. Full file:

```tsx
import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { UserPlus } from "lucide-react";
import { toast } from "sonner";
import {
  createPatient,
  type CreatePatientInput,
} from "@/api/patients";
import { emptyPatientFields } from "@/pages/patients/patient-mappers";
import { GENDER_OPTIONS, MARITAL_STATUS_OPTIONS } from "@/lib/patient-lookups";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field } from "@/components/list";
import { describe } from "@/lib/list-helpers";

export function CreatePatientDialog({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated?: (patientId: string) => void;
}) {
  const queryClient = useQueryClient();
  const [patientCode, setPatientCode] = useState("");
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [middleInitial, setMiddleInitial] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [gender, setGender] = useState<string | null>(null);
  const [maritalStatus, setMaritalStatus] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      setPatientCode("");
      setFirstName("");
      setLastName("");
      setMiddleInitial("");
      setDateOfBirth("");
      setGender(null);
      setMaritalStatus(null);
    }
  }, [open]);

  const mutation = useMutation({
    mutationFn: (input: CreatePatientInput) => createPatient(input),
    onSuccess: (patientId) => {
      toast.success("Patient registered");
      void queryClient.invalidateQueries({ queryKey: ["patients", "list"] });
      onClose();
      onCreated?.(patientId);
    },
    onError: (err) =>
      toast.error("Registration failed", { description: describe(err) }),
  });

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!gender) return;
    mutation.mutate({
      ...emptyPatientFields(),
      patientCode: patientCode.trim(),
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      middleInitial: middleInitial.trim() || null,
      dateOfBirth,
      gender,
      maritalStatus,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>Register a patient</DialogTitle>
            <DialogDescription>
              Capture the essentials now — every other section can be filled in from the
              patient's detail page.
            </DialogDescription>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="pat-code" label="Patient code" required>
              <Input
                id="pat-code"
                value={patientCode}
                onChange={(e) => setPatientCode(e.target.value)}
                placeholder="P-10293"
                autoFocus
                required
              />
            </Field>

            <div className="grid gap-3 sm:grid-cols-[1fr_1fr_72px]">
              <Field id="pat-first" label="First name" required>
                <Input
                  id="pat-first"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Ada"
                  required
                />
              </Field>
              <Field id="pat-last" label="Last name" required>
                <Input
                  id="pat-last"
                  value={lastName}
                  onChange={(e) => setLastName(e.target.value)}
                  placeholder="Lovelace"
                  required
                />
              </Field>
              <Field id="pat-mi" label="M.I.">
                <Input
                  id="pat-mi"
                  value={middleInitial}
                  onChange={(e) => setMiddleInitial(e.target.value)}
                  maxLength={5}
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="pat-dob" label="Date of birth" required>
                <Input
                  id="pat-dob"
                  type="date"
                  value={dateOfBirth}
                  onChange={(e) => setDateOfBirth(e.target.value)}
                  max={new Date().toISOString().slice(0, 10)}
                  required
                />
              </Field>
              <Field id="pat-gender" label="Gender" required>
                <Combobox
                  id="pat-gender"
                  label="Gender"
                  value={gender}
                  onChange={setGender}
                  options={GENDER_OPTIONS}
                  placeholder="Select…"
                  required
                />
              </Field>
            </div>

            <Field id="pat-marital" label="Marital status">
              <Combobox
                id="pat-marital"
                label="Marital status"
                value={maritalStatus}
                onChange={setMaritalStatus}
                options={MARITAL_STATUS_OPTIONS}
                placeholder="Select…"
                clearable
              />
            </Field>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={mutation.isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button
              type="submit"
              disabled={mutation.isPending || !gender}
              className="gap-1.5"
            >
              <UserPlus className="h-4 w-4" />
              {mutation.isPending ? "Registering…" : "Register patient"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

> Note: `createPatient` returns the new patient id (`Promise<string>` — see
> `api/patients.ts`), so `onSuccess`'s first arg is the id.

- [ ] **Step 2: Point `list.tsx` at the shared dialog**

In `clients/dashboard/src/pages/patients/list.tsx`:
1. Delete the entire inline `function CreatePatientDialog(...) { ... }` (the last
   function in the file).
2. Remove now-unused imports that only the dialog used: `useMutation`,
   `useQueryClient`, `FormEvent`, `createPatient`, `CreatePatientInput`,
   `emptyPatientFields`, `GENDER_OPTIONS`, `MARITAL_STATUS_OPTIONS`, `UserPlus`,
   the `Dialog*` cluster, and `Combobox`/`Field` if no longer used elsewhere in
   the file. (Keep imports the page body still uses.)
3. Add at the top with the other imports:

```tsx
import { CreatePatientDialog } from "@/pages/patients/create-patient-dialog";
```

The existing `<CreatePatientDialog open={createOpen} onClose={() => setCreateOpen(false)} />`
usage stays unchanged.

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: no type errors, no unused-import lint errors.

- [ ] **Step 4: Run the existing patients E2E to confirm no regression**

Run: `cd clients/dashboard && npx playwright test tests/patients/patients.spec.ts`
Expected: PASS (the "opens the Register a patient dialog" test still passes with
the extracted component).

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/pages/patients/create-patient-dialog.tsx clients/dashboard/src/pages/patients/list.tsx
git commit -m "refactor(dashboard): extract CreatePatientDialog into shared component

Adds optional onCreated(id) callback for chart-redirect reuse.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Rewrite Patient Chart into a search-first page

**Files:**
- Modify (full rewrite): `clients/dashboard/src/pages/patient-charts/list.tsx`
- Test: `clients/dashboard/tests/patient-charts/search.spec.ts` (create)

**Interfaces:**
- Consumes: `searchPatients` (Task 2), `CreatePatientDialog` with `onCreated`
  (Task 3), `useProviderOptions()` / `useClinicOptions()` from `@/api/administration`
  (both return `ComboboxOption[] | undefined`).
- Produces: the page exported as `PatientChartListPage` (name unchanged — the
  lazy import in `routes.tsx` keeps working).

- [ ] **Step 1: Write the failing E2E spec**

Create `clients/dashboard/tests/patient-charts/search.spec.ts`:

```ts
// E2E for the search-first Patient Chart page: prompt before search,
// results after a query, row → chart navigation, and + New → chart.
import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const ALICE = {
  id: "00000000-0000-0000-0000-0000000a1111",
  patientCode: "P-10293",
  firstName: "Alice",
  lastName: "Vance",
  middleInitial: "Q",
  dateOfBirth: "1990-04-12",
  gender: "F",
  email: "alice.vance@example.com",
  phone: "555-0101",
  isActive: true,
  lastVisitDate: "2026-05-01",
  createdAtUtc: "2026-01-10T10:00:00Z",
  updatedAtUtc: null,
};

test.describe("patient chart — search-first", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
  });

  test("shows the prompt before any search and no patient rows", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await page.goto("/patient-charts");

    await expect(
      page.getByRole("heading", { name: "Patient Chart", level: 1 }),
    ).toBeVisible();
    await expect(page.getByText(/search for a patient to open their chart/i)).toBeVisible();
    // Default (no filter active) must NOT list patients.
    await expect(page.getByText("Alice Q Vance")).toHaveCount(0);
  });

  test("typing a search shows matching rows", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await page.goto("/patient-charts");

    await page.getByPlaceholder(/search by name or patient code/i).fill("Vance");
    await expect(page.getByText("Alice Q Vance").last()).toBeVisible();
    await expect(page.getByText("P-10293").last()).toBeVisible();
  });

  test("clicking a result navigates to the patient chart", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([ALICE]));
    await page.goto("/patient-charts");

    await page.getByPlaceholder(/search by name or patient code/i).fill("Vance");
    await page.getByRole("link", { name: /open chart for alice q vance/i }).first().click();

    await expect(page).toHaveURL(new RegExp(`/patient-charts/${ALICE.id}$`));
  });
});
```

- [ ] **Step 2: Run the spec to verify it fails**

Run: `cd clients/dashboard && npx playwright test tests/patient-charts/search.spec.ts`
Expected: FAIL — current page lists all patients (the prompt text and the
"no rows before search" assertion fail).

- [ ] **Step 3: Rewrite `list.tsx` as the search-first page**

Replace the entire contents of `clients/dashboard/src/pages/patient-charts/list.tsx`:

```tsx
import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { ChevronRight, ClipboardList, Plus, Search } from "lucide-react";
import { searchPatients, type PatientListItemDto } from "@/api/patients";
import { useClinicOptions, useProviderOptions } from "@/api/administration";
import { CreatePatientDialog } from "@/pages/patients/create-patient-dialog";
import { Button } from "@/components/ui/button";
import {
  Combobox,
  EntityEmpty,
  EntityFilterPill,
  EntityInitialsAvatar,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityMobileCard,
  EntityPageHeader,
  EntityPager,
  EntitySearch,
} from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";

const PAGE_SIZE = 20;
const DESKTOP_COLS = "grid-cols-[1.4fr_120px_120px_24px] lg:grid-cols-[1.6fr_120px_120px_120px_24px]";

type StatusFilter = "all" | "active" | "inactive";

function fullName(p: PatientListItemDto): string {
  return [p.firstName, p.middleInitial, p.lastName].filter(Boolean).join(" ");
}

export function PatientChartListPage() {
  const navigate = useNavigate();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [providerId, setProviderId] = useState<string | null>(null);
  const [clinicId, setClinicId] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [pageNumber, setPageNumber] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);

  const providerOptions = useProviderOptions();
  const clinicOptions = useClinicOptions();

  useEffect(() => {
    const t = setTimeout(() => {
      setDebouncedSearch(search.trim());
      setPageNumber(1);
    }, 250);
    return () => clearTimeout(t);
  }, [search]);

  useEffect(() => {
    setPageNumber(1);
  }, [providerId, clinicId, statusFilter]);

  // The page is search-first: only fetch once at least one filter is active.
  const hasActiveFilter =
    debouncedSearch.length > 0 ||
    providerId !== null ||
    clinicId !== null ||
    statusFilter !== "all";

  const queryParams = useMemo(
    () => ({
      pageNumber,
      pageSize: PAGE_SIZE,
      search: debouncedSearch || undefined,
      providerId: providerId ?? undefined,
      clinicId: clinicId ?? undefined,
      isActive: statusFilter === "all" ? null : statusFilter === "active",
      sortBy: "lastName",
      sortDir: "asc" as const,
    }),
    [pageNumber, debouncedSearch, providerId, clinicId, statusFilter],
  );

  const query = useQuery({
    queryKey: ["patient-charts", "search", queryParams],
    queryFn: () => searchPatients(queryParams),
    enabled: hasActiveFilter,
    placeholderData: keepPreviousData,
  });

  const data = query.data;
  const items = hasActiveFilter ? data?.items ?? [] : [];

  const clearFilters = () => {
    setSearch("");
    setProviderId(null);
    setClinicId(null);
    setStatusFilter("all");
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={ClipboardList}
        title="Patient Chart"
        total={hasActiveFilter ? data?.totalCount ?? null : null}
        unit="patient"
        description="Search for a patient to open their clinical chart, or register a new one."
      >
        <Button
          onClick={() => setCreateOpen(true)}
          className="h-9 flex-1 gap-1.5 rounded-lg px-4 text-[13px] font-semibold sm:flex-none"
        >
          <Plus className="size-4" />
          New patient
        </Button>
      </EntityPageHeader>

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder="Search by name or patient code…"
      />

      <div className="flex flex-wrap items-center gap-3">
        <div className="w-52">
          <Combobox
            id="filter-provider"
            label="Provider"
            value={providerId}
            onChange={setProviderId}
            options={providerOptions ?? []}
            placeholder="All providers"
            clearable
          />
        </div>
        <div className="w-52">
          <Combobox
            id="filter-clinic"
            label="Clinic"
            value={clinicId}
            onChange={setClinicId}
            options={clinicOptions ?? []}
            placeholder="All clinics"
            clearable
          />
        </div>
        <EntityFilterPill
          label="Status"
          value={statusFilter}
          onChange={setStatusFilter}
          options={[
            { value: "all", label: "All" },
            { value: "active", label: "Active" },
            { value: "inactive", label: "Inactive" },
          ]}
        />
        {hasActiveFilter && (
          <Button
            variant="outline"
            onClick={clearFilters}
            className="h-9 rounded-lg px-4 text-[13px]"
          >
            Clear
          </Button>
        )}
      </div>
      <p className="text-[11px] text-[var(--color-muted-foreground)]">
        Provider / Clinic match patients who have a report for that provider or clinic.
      </p>

      {!hasActiveFilter ? (
        <EntityEmpty
          icon={Search}
          title="Search for a patient"
          body="Search for a patient to open their chart, or register a new one."
          action={
            <Button
              onClick={() => setCreateOpen(true)}
              className="h-9 rounded-lg px-4 text-[13px]"
            >
              <Plus className="mr-1.5 size-4" />
              New patient
            </Button>
          }
        />
      ) : query.isLoading && items.length === 0 ? (
        <EntityListLoading desktopColumns={DESKTOP_COLS} />
      ) : items.length === 0 ? (
        <EntityEmpty
          icon={ClipboardList}
          title="No patients found"
          body="Nothing matches the current search and filters."
        />
      ) : (
        <div>
          {/* Mobile */}
          <div className="space-y-2 md:hidden">
            {items.map((p) => (
              <EntityMobileCard
                key={p.id}
                href={`/patient-charts/${p.id}`}
                aria-label={`Open chart for ${fullName(p)}`}
              >
                <div className="flex items-center justify-between">
                  <div className="flex min-w-0 items-center gap-3">
                    <EntityInitialsAvatar name={fullName(p)} size={40} />
                    <div className="min-w-0">
                      <p className="truncate text-[14px] font-medium">{fullName(p)}</p>
                      <p className="mt-0.5 text-[11px] text-[var(--color-muted-foreground)]">
                        {p.patientCode}
                      </p>
                    </div>
                  </div>
                  <ChevronRight className="size-4 shrink-0 text-[var(--color-border)]" />
                </div>
              </EntityMobileCard>
            ))}
          </div>

          {/* Desktop */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_COLS}>
              <span>Name</span>
              <span>Patient code</span>
              <span>Date of birth</span>
              <span className="hidden lg:block">Last visit</span>
              <span />
            </EntityListHeader>

            {items.map((p, i) => (
              <EntityListRow key={p.id} className={DESKTOP_COLS} isLast={i === items.length - 1}>
                <div className="flex items-center gap-3 min-w-0">
                  <EntityInitialsAvatar name={fullName(p)} size={32} />
                  <span className="truncate text-[13px] font-medium">{fullName(p)}</span>
                </div>
                <span className="text-[13px] text-[var(--color-muted-foreground)]">
                  {p.patientCode}
                </span>
                <span className="text-[13px] text-[var(--color-muted-foreground)]">
                  {formatDate(p.dateOfBirth)}
                </span>
                <span className="hidden text-[13px] text-[var(--color-muted-foreground)] lg:block">
                  {formatDate(p.lastVisitDate)}
                </span>
                <Link
                  to={`/patient-charts/${p.id}`}
                  aria-label={`Open chart for ${fullName(p)}`}
                  className="flex items-center justify-end"
                >
                  <ChevronRight className="size-4 text-[var(--color-border)] group-hover:text-[var(--color-foreground)]" />
                </Link>
              </EntityListRow>
            ))}
          </EntityListCard>

          <EntityPager
            page={data?.pageNumber ?? 1}
            totalPages={Math.max(data?.totalPages ?? 1, 1)}
            hasPrev={data?.hasPrevious ?? false}
            hasNext={data?.hasNext ?? false}
            onPrev={() => setPageNumber((p) => Math.max(1, p - 1))}
            onNext={() => setPageNumber((p) => p + 1)}
          />
        </div>
      )}

      {query.isError && (
        <div
          role="alert"
          className="rounded-lg border border-[oklch(from_var(--color-destructive)_l_c_h_/_0.30)] bg-[oklch(from_var(--color-destructive)_l_c_h_/_0.06)] px-3 py-2 text-sm text-[var(--color-destructive)]"
        >
          {describe(query.error)}
        </div>
      )}

      <CreatePatientDialog
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(id) => navigate(`/patient-charts/${id}`)}
      />
    </div>
  );
}
```

> Verify prop names against the current `@/components/list` barrel and
> `@/api/administration` before finishing: `EntitySearch`, `EntityFilterPill`,
> `Combobox` (`value/onChange/options/placeholder/clearable`),
> `EntityPager` (`hasPrev/hasNext`), `useProviderOptions`, `useClinicOptions`.
> These are all used as shown elsewhere (`patients/list.tsx`, `patient-charts/chart.tsx`).

- [ ] **Step 4: Run the spec to verify it passes**

Run: `cd clients/dashboard && npx playwright test tests/patient-charts/search.spec.ts`
Expected: PASS (all three tests).

- [ ] **Step 5: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/pages/patient-charts/list.tsx clients/dashboard/tests/patient-charts/search.spec.ts
git commit -m "feat(dashboard): search-first Patient Chart page with provider/clinic filters

Replaces the all-patients list with a search/filter screen; empty until a
filter is active; row opens the chart; + New routes to the new chart.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Add "Edit patient info" link on the chart

**Files:**
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx` (Patient Info card header, around lines 380-389; imports)

**Interfaces:**
- Consumes: `patientId` from `useParams` (already in scope in this component).

- [ ] **Step 1: Ensure `Pencil` and `Link` are imported**

`Link` is already imported from `react-router-dom` (top of file). `Pencil` is
already imported from `lucide-react` (used by `IconShortcut` callers). No new
imports needed — confirm both are present.

- [ ] **Step 2: Add the edit link to the Patient Info card header**

In `chart.tsx`, the Patient Info card header currently renders the title +
status badge:

```tsx
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  Patient Info
                </h2>
                <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
                  {patient.isActive ? "Active" : "Inactive"}
                </EntityStatusBadge>
              </div>
```

Replace the `<h2>…</h2>` line's sibling layout so the status badge and an edit
link sit together — change the closing of that header block to:

```tsx
              <div className="mb-3 flex items-center justify-between">
                <h2 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-primary)]">
                  Patient Info
                </h2>
                <div className="flex items-center gap-2">
                  <EntityStatusBadge tone={patient.isActive ? "success" : "default"}>
                    {patient.isActive ? "Active" : "Inactive"}
                  </EntityStatusBadge>
                  <Link
                    to={`/patients/${patientId}`}
                    title="Edit patient info"
                    aria-label="Edit patient info"
                    className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-foreground)] transition-colors hover:bg-[var(--color-accent)]"
                  >
                    <Pencil className="size-4" />
                  </Link>
                </div>
              </div>
```

- [ ] **Step 3: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

- [ ] **Step 4: Add an assertion to the chart E2E (if a chart spec exists) or a minimal new test**

If `clients/dashboard/tests/patient-charts/` already has a chart spec that loads a
patient, add:

```ts
await expect(page.getByRole("link", { name: /edit patient info/i })).toHaveAttribute(
  "href",
  new RegExp(`/patients/${PATIENT_ID}$`),
);
```

If no chart spec exists, skip — Task 4's spec plus the build cover the change;
do not scaffold a new chart harness here.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/pages/patient-charts/chart.tsx clients/dashboard/tests/patient-charts
git commit -m "feat(dashboard): edit patient demographics from the chart

Adds an Edit patient info link on the Patient Info card -> /patients/:id.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Repoint the demographics detail page at the chart hub

**Files:**
- Modify: `clients/dashboard/src/pages/patients/patient-detail.tsx` (lines 133, 361, 811)

**Interfaces:**
- Consumes: `patientId` (already `const { patientId = "" } = useParams(...)` at line 119).

- [ ] **Step 1: Repoint the top back link (line 133)**

Replace:

```tsx
      <EntityDetailBack to="/patients" label="Back to patients" />
```

with:

```tsx
      <EntityDetailBack to={`/patient-charts/${patientId}`} label="Back to chart" />
```

- [ ] **Step 2: Repoint the after-delete redirect (line 361)**

Replace:

```tsx
            onDeleted={() => navigate("/patients")}
```

with:

```tsx
            onDeleted={() => navigate("/patient-charts")}
```

(The patient no longer exists after deletion, so return to the search page.)

- [ ] **Step 3: Repoint the not-found back link (line 811)**

In `NotFoundPanel` (no `patientId` in scope there), replace:

```tsx
        <Link to="/patients">Back to patients</Link>
```

with:

```tsx
        <Link to="/patient-charts">Back to Patient Chart</Link>
```

- [ ] **Step 4: Update the patients detail E2E expectations**

In `clients/dashboard/tests/patients/patients.spec.ts`, the adult-detail test asserts
`getByRole("link", { name: /back to patients/i })`. Update that assertion to:

```ts
    await expect(page.getByRole("link", { name: /back to chart/i })).toBeVisible();
```

- [ ] **Step 5: Run the patients E2E**

Run: `cd clients/dashboard && npx playwright test tests/patients/patients.spec.ts`
Expected: PASS with the updated back-link assertion.

- [ ] **Step 6: Typecheck + lint, then commit**

Run: `cd clients/dashboard && npm run build && npm run lint`

```bash
git add clients/dashboard/src/pages/patients/patient-detail.tsx clients/dashboard/tests/patients/patients.spec.ts
git commit -m "refactor(dashboard): point patient detail navigation at the chart hub

Back link -> patient's chart; after-delete -> Patient Chart search.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Remove the standalone Patients menu item and list page

**Files:**
- Delete: `clients/dashboard/src/pages/patients/list.tsx`
- Modify: `clients/dashboard/src/routes.tsx` (lines 176, ~295)
- Modify: `clients/dashboard/src/components/layout/nav-data.ts` (lines 84-91)

**Interfaces:**
- The `patients/:patientId` route and `PatientDetailPage` lazy import are KEPT.

- [ ] **Step 1: Delete the list page file**

```bash
git rm clients/dashboard/src/pages/patients/list.tsx
```

- [ ] **Step 2: Remove the lazy import and route in `routes.tsx`**

Delete the `PatientsListPage` lazy import (line 176):

```tsx
const PatientsListPage = lazyNamed(() => import("@/pages/patients/list"), "PatientsListPage");
```

Delete the list route line (in the routes array, ~line 295):

```tsx
          { path: "patients", element: withSuspense(<PatientsListPage />) },
```

Keep `{ path: "patients/:patientId", element: withSuspense(<PatientDetailPage />) }`.

- [ ] **Step 3: Collapse the nav section to a single Patient Chart entry**

In `nav-data.ts`, change the `patients` section's `items` (lines 87-90) from two
entries to one — remove the `/patients` item, keep Patient Chart:

```ts
    items: [
      { to: "/patient-charts", label: "Patient Chart", icon: ClipboardList, perm: INCIDENT_PERMISSIONS.view },
    ],
```

If `Stethoscope` and `PATIENT_PERMISSIONS` are now unused in `nav-data.ts`, remove
those imports to satisfy the lint/warnings-as-errors. (Check: `Stethoscope` is also
the section `icon` on line 86 — if so, keep its import. `PATIENT_PERMISSIONS` was
only used by the removed item — remove it if no longer referenced.)

- [ ] **Step 4: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean — no dangling imports, no references to `PatientsListPage` or the
removed route.

- [ ] **Step 5: Confirm no other references to the removed list route**

Run: `cd clients/dashboard && npx playwright test tests/patients/patients.spec.ts`
Expected: the `patients — list` describe block now targets a non-existent route.
Remove that describe block's three tests (`renders the heading…`,
`shows the empty state…`, `opens the Register a patient dialog…`) — list-page
coverage moved to `tests/patient-charts/search.spec.ts` (search) and Task 3
(dialog). Keep the `patients/:patientId — detail` blocks. Re-run:

Run: `cd clients/dashboard && npx playwright test tests/patients/patients.spec.ts`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add clients/dashboard/src/routes.tsx clients/dashboard/src/components/layout/nav-data.ts clients/dashboard/src/pages/patients/list.tsx clients/dashboard/tests/patients/patients.spec.ts
git commit -m "feat(dashboard): single Patient Chart menu; remove Patients list page

Patient Chart is the only patient entry point; demographics editing is
reached from the chart. Removes the redundant /patients list route + nav item.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 8: Full suite verification

**Files:** none (verification only)

- [ ] **Step 1: Backend build + arch tests**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: 0 warnings, 0 errors.

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj`
Expected: PASS — all 51 tests. The 3 formerly-failing tests are now green
(Task 1 added the SearchPatients validator; Task 10 recognized the "Reschedule"
endpoint verb; Task 11 gave PatientDbContext the canonical constructor).

- [ ] **Step 2: Frontend build, lint, full E2E**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

Run: `cd clients/dashboard && npm run test:e2e`
Expected: PASS (all specs, including the new `patient-charts/search.spec.ts` and
the trimmed `patients/patients.spec.ts`).

- [ ] **Step 3: Manual smoke (optional but recommended)**

Start the API (`dotnet run --project src/Host/FSH.Starter.Api`) and dashboard
(`cd clients/dashboard && npm run dev`). Verify: sidebar shows one "Patient Chart"
item; the page is empty until you search; a provider/clinic filter narrows
results; a row opens the chart; the chart's Patient Info card has an Edit link to
the demographics page; that page's Back returns to the chart.

---

### Task 9: Docs + changelog (golden rule 10)

**Files:**
- Modify: the separate docs repo (`github.com/fullstackhero/docs`) — patient/chart pages describing the navigation.
- Create: a changelog entry under `src/content/docs/changelog/` in that docs repo.

- [ ] **Step 1: Update the docs site**

In the docs repo, update any page that documents the dashboard patient navigation
to describe: a single "Patient Chart" menu item, search-first entry with
name/code + provider + clinic + status filters, and demographics editing reached
from the chart's "Edit patient info" link. Note the provider/clinic filter
semantics ("patients with a report for that provider/clinic").

- [ ] **Step 2: Add a changelog entry**

Add a dated entry under `src/content/docs/changelog/` summarizing the change
(modernized Patient Chart search; consolidated patient navigation).

- [ ] **Step 3: Commit in the docs repo**

```bash
git add src/content/docs
git commit -m "docs: modernized Patient Chart search + consolidated patient nav"
```

> If the docs repo is not checked out locally, flag this task to the user as
> follow-up rather than skipping it — the golden rule requires docs to travel
> with the change.

---

### Task 10: Recognize "Reschedule" as an endpoint action verb (pre-existing arch fix)

**Files:**
- Modify: `src/Tests/Architecture.Tests/EndpointConventionTests.cs` (the `hasVerb` chain, ~lines 228-286)

**Context:** `Endpoint_Names_Should_Follow_Convention` fails today because
`FSH.Modules.Scheduling.Features.v1.Appointments.RescheduleAppointment.RescheduleAppointmentEndpoint`
starts with "Reschedule", which is a legitimate action verb (like the already-listed
"Cancel", "NoShow", "Confirm") but is missing from the allow-list. Lowest-risk fix:
add it to the list — no endpoint/route/class rename, so no scheduling-client impact.

- [ ] **Step 1: Run the failing arch test to confirm the violation**

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj --filter "FullyQualifiedName~EndpointConventionTests"`
Expected: FAIL with `RescheduleAppointmentEndpoint name should start with an action verb`.

- [ ] **Step 2: Add "Reschedule" to the recognized-verb chain**

In `EndpointConventionTests.cs`, in the `bool hasVerb = ...` chain, add a line
(place it next to the other appointment verbs, e.g. immediately after the
`name.StartsWith("NoShow", StringComparison.Ordinal) ||` line):

```csharp
                               name.StartsWith("Reschedule", StringComparison.Ordinal) ||
```

- [ ] **Step 3: Run the arch test to confirm it passes**

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj --filter "FullyQualifiedName~EndpointConventionTests"`
Expected: PASS.

- [ ] **Step 4: Commit**

```bash
git add src/Tests/Architecture.Tests/EndpointConventionTests.cs
git commit -m "test(arch): recognize Reschedule as an endpoint action verb

Clears the pre-existing EndpointConventionTests failure for
RescheduleAppointmentEndpoint; no endpoint rename.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 11: Give PatientDbContext the canonical BaseDbContext constructor (pre-existing arch fix)

**Files:**
- Modify: `src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs`
- Test: `src/Tests/Patient.Tests/Infrastructure/PatientDbContextConstructorTests.cs` (create)

**Context:** `TenantIsolationTests.BaseDbContext_Entities_Should_Be_TenantIsolated_Or_Marked_Global`
fails because it reflects for the canonical 4-arg `BaseDbContext` ctor
`(IMultiTenantContextAccessor<AppTenantInfo>, DbContextOptions<PatientDbContext>, IOptions<DatabaseOptions>, IHostEnvironment)`
and `PatientDbContext` only exposes a 5-arg ctor (extra `IPhiEncryptor`). The
encryptor is used in `OnModelCreating` (`new PatientConfiguration(_phi)`), so the
model needs a non-null encryptor to build.

**Approach:** Add a second PUBLIC 4-arg ctor that chains to the 5-arg one with a
no-op passthrough encryptor. .NET DI greedily selects the *5-arg* ctor at runtime
(its parameter set is a strict superset and every param is registered), so
production always gets the real `IPhiEncryptor`. The 4-arg ctor is used only by
the arch test's explicit reflection (and never reads/writes PHI — it only inspects
`ctx.Model` metadata). A guard test proves DI still wires the real encryptor.

- [ ] **Step 1: Write the failing guard test**

Create `src/Tests/Patient.Tests/Infrastructure/PatientDbContextConstructorTests.cs`.
This test builds a service provider mirroring the real registration and asserts the
resolved `PatientDbContext` holds the **real** registered `IPhiEncryptor` (i.e. DI
picked the 5-arg ctor), then separately asserts the canonical 4-arg ctor exists
(what the arch test needs):

```csharp
using System.Reflection;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Infrastructure;

public sealed class PatientDbContextConstructorTests
{
    [Fact]
    public void Has_The_Canonical_Four_Arg_BaseDbContext_Constructor()
    {
        var ctor = typeof(PatientDbContext).GetConstructor(
        [
            typeof(IMultiTenantContextAccessor<AppTenantInfo>),
            typeof(DbContextOptions<PatientDbContext>),
            typeof(IOptions<DatabaseOptions>),
            typeof(IHostEnvironment),
        ]);

        ctor.ShouldNotBeNull(
            "Architecture.Tests reflects for this exact signature to construct the context.");
    }

    [Fact]
    public void Di_Resolves_The_Context_With_The_Real_Phi_Encryptor()
    {
        var realEncryptor = Substitute.For<IPhiEncryptor>();

        var services = new ServiceCollection();
        services.AddSingleton<IMultiTenantContextAccessor<AppTenantInfo>>(
            Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>());
        services.AddSingleton(Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        }));
        services.AddSingleton<IHostEnvironment>(new HostingEnvironment { EnvironmentName = "Development" });
        services.AddSingleton(realEncryptor);
        services.AddDbContext<PatientDbContext>(o =>
            o.UseNpgsql("Host=arch;Database=arch;Username=arch;Password=arch"));

        using var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<PatientDbContext>();

        // Reflect the private _phi field: must be the registered real encryptor,
        // proving DI selected the 5-arg ctor (not the no-op 4-arg test ctor).
        var phiField = typeof(PatientDbContext).GetField("_phi", BindingFlags.Instance | BindingFlags.NonPublic);
        phiField.ShouldNotBeNull();
        phiField.GetValue(ctx).ShouldBeSameAs(realEncryptor);
    }
}
```

- [ ] **Step 2: Run the guard test to verify it fails to compile/pass**

Run: `dotnet test src/Tests/Patient.Tests/Patient.Tests.csproj --filter "FullyQualifiedName~PatientDbContextConstructorTests"`
Expected: FAIL — `Has_The_Canonical_Four_Arg_BaseDbContext_Constructor` fails
(no such ctor) and the DI test may throw on ambiguous/no ctor.

- [ ] **Step 3: Add the canonical 4-arg constructor + no-op encryptor**

In `PatientDbContext.cs`, after the existing 5-arg constructor (ending at the
`_phi = phi;` block, line ~28), add:

```csharp
    /// <summary>
    /// Canonical <see cref="BaseDbContext"/> constructor (no <see cref="IPhiEncryptor"/>),
    /// present so design-time tooling and <c>Architecture.Tests</c>'
    /// <c>TenantIsolationTests</c> can construct the context to inspect its model.
    /// Production never selects this overload: the .NET DI container greedily binds
    /// the 5-arg constructor above (a strict superset whose every parameter is
    /// registered), so the real encryptor is always used. The no-op encryptor here
    /// only ever participates in model-metadata inspection, never PHI read/write.
    /// </summary>
    public PatientDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PatientDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : this(multiTenantContextAccessor, options, settings, environment, NoOpPhiEncryptor.Instance)
    {
    }

    /// <summary>Identity encryptor used only by the design-time/test constructor above.</summary>
    private sealed class NoOpPhiEncryptor : IPhiEncryptor
    {
        public static readonly NoOpPhiEncryptor Instance = new();
        public string? Encrypt(string? plaintext) => plaintext;
        public string? Decrypt(string? ciphertext) => ciphertext;
    }
```

> Before writing, open `src/Modules/Patient/Modules.Patient/Infrastructure/PhiEncryptor.cs`
> and the `IPhiEncryptor` interface and match the EXACT member signatures
> (method names, nullability, parameters). The `Encrypt`/`Decrypt` shapes shown
> are the expected ones; correct them to the interface if it differs (e.g.
> different method names or a search-hash member). The no-op must implement every
> interface member as a passthrough/no-op.

- [ ] **Step 4: Build + run the guard test**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: 0 warnings/0 errors.

Run: `dotnet test src/Tests/Patient.Tests/Patient.Tests.csproj --filter "FullyQualifiedName~PatientDbContextConstructorTests"`
Expected: PASS — both tests (canonical ctor exists; DI still wires the real encryptor).

- [ ] **Step 5: Run the tenant-isolation arch test**

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj --filter "FullyQualifiedName~TenantIsolationTests"`
Expected: PASS — PatientDbContext now constructs via the canonical ctor.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs src/Tests/Patient.Tests/Infrastructure/PatientDbContextConstructorTests.cs
git commit -m "fix(patient): add canonical BaseDbContext constructor to PatientDbContext

Clears the pre-existing TenantIsolationTests failure. DI still greedily binds
the 5-arg ctor (real IPhiEncryptor); guard test proves it. The no-op encryptor
only serves design-time model inspection.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:**
- Single Patient Chart menu → Task 7. ✓
- Demographics reached from chart → Task 5; detail nav repointed → Task 6. ✓
- Free-text + status + provider + clinic filters → Task 1 (backend), Task 2 (api), Task 4 (UI). ✓
- Provider/clinic = in-module reports EXISTS → Task 1, Step 2. ✓
- Search-first (empty until filter active) → Task 4 (`hasActiveFilter`) + spec test. ✓
- + New → chart → Task 3 (`onCreated`), Task 4 (`navigate`). ✓
- Shared create dialog extracted → Task 3. ✓
- Soft-deleted reports excluded → Task 1 (`!r.IsDeleted`). ✓
- Helper tooltip on provider/clinic meaning → Task 4 (the `<p>` under filters). ✓
- No migration / no endpoint added / no BuildingBlocks → Tasks 1, Global Constraints. ✓
- Docs + changelog → Task 9. ✓
- Pre-existing arch failures (user chose "fix all 3"): SearchPatients validator → Task 1 Step 4; "Reschedule" endpoint verb → Task 10; PatientDbContext canonical ctor + DI guard → Task 11. Architecture.Tests goes 3-fail → 0-fail (verified in Task 8). ✓

**Placeholder scan:** No TBD/TODO; every code step shows full code. ✓

**Type consistency:** `CreatePatientDialog({open,onClose,onCreated?})` defined in Task 3 and consumed in Task 4 with the same shape; `searchPatients` param names (`providerId`/`clinicId`) consistent across Tasks 1, 2, 4; page export name `PatientChartListPage` unchanged so `routes.tsx` lazy import stays valid. ✓
