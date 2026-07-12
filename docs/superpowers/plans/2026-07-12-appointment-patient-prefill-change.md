# Appointment patient pre-fill + "Change" search — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Pre-fill the appointment patient field from the current patient (chart button + active patient tab), show a selected patient as a collapsed "name + Change" chip, and move all patient searching into a nested BackChart-style popup dialog.

**Architecture:** Extract the typeahead search out of `PatientPicker` into a new `PatientSearchDialog` modal. `PatientPicker` keeps its public props but now renders a collapsed chip (name + Change + Clear) when a patient is set, or a "Search for patient" button when empty — both open the dialog. `appointments.tsx` seeds the create dialog's patient from `useActivePatientTab()`.

**Tech Stack:** React 19, TypeScript, TanStack Query v5, Radix Dialog, Tailwind v4, Playwright (route-mocked E2E).

## Global Constraints

- Frontend app: `clients/dashboard`. Run commands from that directory.
- No new dependencies; reuse `searchPatients` from `@/api/patients`.
- `PatientPicker`'s public props stay exactly: `{ value, initialLabel, onChange, disabled }`.
- Tests are Playwright, route-mocked, in `tests/scheduling/appointments.spec.ts`. Run with `npx playwright test appointments.spec`.
- Strict build: `npx tsc -b --noEmit` must pass (unused imports fail as TS6133).
- Commit per task (this repo commits one logical change per task).

---

### Task 1: `PatientSearchDialog` + collapse `PatientPicker`

**Files:**
- Create: `clients/dashboard/src/components/scheduling/patient-search-dialog.tsx`
- Modify: `clients/dashboard/src/components/scheduling/patient-picker.tsx` (full rewrite of internals; keep exported `patientLabel` + `PatientPicker` signature)
- Modify: `clients/dashboard/src/pages/scheduling/appointments.tsx` (pass `disabled={readOnly}` to `PatientPicker`)
- Test: `clients/dashboard/tests/scheduling/appointments.spec.ts`

**Interfaces:**
- Consumes: `searchPatients`, `PatientListItemDto` from `@/api/patients`; `Dialog`, `DialogContent`, `DialogHeader`, `DialogTitle`, `DialogDescription` from `@/components/ui/dialog`; `Input`, `Button`.
- Produces:
  - `PatientSearchDialog({ open, onOpenChange, onSelect }: { open: boolean; onOpenChange: (open: boolean) => void; onSelect: (patient: PatientListItemDto) => void })`
  - `PatientPicker({ value, initialLabel, onChange, disabled })` — unchanged signature.
  - `patientLabel(p) => "Last, First · CODE"` — unchanged, still exported.

- [ ] **Step 1: Update the existing empty-state assertion (failing test)**

In `tests/scheduling/appointments.spec.ts`, the test `"New appointment dialog has patient, type, reserve toggle, and times"` currently asserts an inline search input. Replace that assertion so it expects the new button. Change:

```typescript
    // Patient search input is present.
    await expect(dialog.getByPlaceholder(/search by name or code/i)).toBeVisible();
```

to:

```typescript
    // Empty patient state is a "Search for patient" button (opens the search popup).
    await expect(dialog.getByRole("button", { name: /search for patient/i })).toBeVisible();
```

- [ ] **Step 2: Add the search-and-pick test (failing)**

Add this test inside the `test.describe("scheduling/appointments", …)` block:

```typescript
  test("New appointment: searching and picking a patient collapses to a name + Change", async ({ page }) => {
    const patient = {
      id: "00000000-0000-0000-0000-0000000000a1",
      patientCode: "P-1001",
      firstName: "Jane",
      lastName: "Doe",
      dateOfBirth: "1990-04-05",
      isActive: true,
    };
    await mockScheduling(page);
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([patient], { pageSize: 8 }));
    await page.goto("/scheduling/appointments");
    await page.getByRole("button", { name: /new appointment/i }).click();

    // Empty state → open the nested search popup.
    await page.getByRole("dialog").getByRole("button", { name: /search for patient/i }).click();

    // Only the search popup has a text input; type and pick the result.
    await page.getByPlaceholder(/search by name or code/i).fill("Doe");
    await page.getByRole("option", { name: /Doe, Jane/ }).click();

    // Collapses to a read-only chip with the patient name + a Change button.
    await expect(page.getByText("Doe, Jane · P-1001")).toBeVisible();
    await expect(page.getByRole("button", { name: /^change$/i })).toBeVisible();
  });
```

- [ ] **Step 3: Run the tests to verify they fail**

Run: `cd clients/dashboard && npx playwright test appointments.spec --grep "reserve toggle|collapses to a name"`
Expected: FAIL — the "Search for patient" button does not exist yet (empty state still renders the inline input).

- [ ] **Step 4: Create `PatientSearchDialog`**

Create `clients/dashboard/src/components/scheduling/patient-search-dialog.tsx`:

```tsx
import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Loader2, Search } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { searchPatients, type PatientListItemDto } from "@/api/patients";
import { cn } from "@/lib/cn";

function fmtDob(iso: string): string {
  return new Intl.DateTimeFormat("en-US", { year: "numeric", month: "short", day: "numeric" }).format(
    new Date(iso),
  );
}

/**
 * PatientSearchDialog — a modal debounced typeahead over /patient/patients
 * (no PHI in the list response). Mirrors BackChart's "Search for Patient"
 * popup: searching happens here, not inline in the appointment form. Picking
 * a row fires `onSelect` and closes.
 */
export function PatientSearchDialog({
  open,
  onOpenChange,
  onSelect,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSelect: (patient: PatientListItemDto) => void;
}) {
  const [query, setQuery] = useState("");
  const [debounced, setDebounced] = useState("");

  // Reset the query each time the dialog closes so the next open starts clean.
  useEffect(() => {
    if (!open) {
      setQuery("");
      setDebounced("");
    }
  }, [open]);

  useEffect(() => {
    const t = setTimeout(() => setDebounced(query.trim()), 250);
    return () => clearTimeout(t);
  }, [query]);

  const resultsQuery = useQuery({
    queryKey: ["scheduling", "patients", "search", debounced],
    queryFn: () => searchPatients({ search: debounced, isActive: true, pageNumber: 1, pageSize: 8 }),
    enabled: debounced.length > 0 && open,
    staleTime: 30_000,
  });
  const results = useMemo(() => resultsQuery.data?.items ?? [], [resultsQuery.data]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="!max-w-md">
        <DialogHeader>
          <DialogTitle>Search for patient</DialogTitle>
          <DialogDescription>Find a patient by name or code.</DialogDescription>
        </DialogHeader>
        <div className="space-y-2 px-6 pb-6 pt-1">
          <div className="relative">
            <Search
              aria-hidden
              className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-[var(--color-muted-foreground)]"
            />
            <Input
              type="search"
              placeholder="Search by name or code…"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              autoComplete="off"
              spellCheck={false}
              autoFocus
              className="pl-8"
            />
          </div>

          <div className="max-h-64 overflow-auto rounded-md border border-[var(--color-border)]">
            {debounced.length === 0 ? (
              <div className="px-3 py-3 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
                Type to search patients.
              </div>
            ) : resultsQuery.isFetching && results.length === 0 ? (
              <div className="flex items-center gap-2 px-3 py-2.5 text-[12.5px] text-[var(--color-muted-foreground)]">
                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                Searching for "{debounced}"…
              </div>
            ) : results.length === 0 ? (
              <div className="px-3 py-3 text-center text-[12.5px] text-[var(--color-muted-foreground)]">
                No patients match "{debounced}".
              </div>
            ) : (
              <div role="listbox" aria-label="Patient search results">
                {results.map((p) => (
                  <button
                    key={p.id}
                    type="button"
                    role="option"
                    aria-selected={false}
                    onClick={() => onSelect(p)}
                    className={cn(
                      "flex w-full items-center gap-2.5 px-3 py-2 text-left transition-colors",
                      "hover:bg-[var(--color-muted)] focus-visible:bg-[var(--color-muted)] focus-visible:outline-none",
                    )}
                  >
                    <span
                      aria-hidden
                      className="grid h-6 w-6 shrink-0 place-items-center rounded-full bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
                    >
                      {(p.lastName?.[0] ?? "?").toUpperCase()}
                    </span>
                    <div className="min-w-0 flex-1">
                      <div className="truncate text-[13px] font-medium tracking-tight">
                        {p.lastName}, {p.firstName}
                      </div>
                      <div className="truncate font-mono text-[10.5px] text-[var(--color-muted-foreground)]">
                        {p.patientCode} · DOB {fmtDob(p.dateOfBirth)}
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 5: Rewrite `PatientPicker` to the collapsed control**

Replace the entire contents of `clients/dashboard/src/components/scheduling/patient-picker.tsx` with:

```tsx
import { useEffect, useState } from "react";
import { Search, UserRound, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { PatientSearchDialog } from "@/components/scheduling/patient-search-dialog";
import { type PatientListItemDto } from "@/api/patients";

/** Display label for a patient row: "Last, First · CODE". */
export function patientLabel(p: { firstName: string; lastName: string; patientCode: string }): string {
  return `${p.lastName}, ${p.firstName} · ${p.patientCode}`;
}

function fmtDob(iso: string): string {
  return new Intl.DateTimeFormat("en-US", { year: "numeric", month: "short", day: "numeric" }).format(
    new Date(iso),
  );
}

/**
 * PatientPicker — a collapsed patient control. A selected patient renders as a
 * read-only chip (name + DOB) with Change/Clear; empty renders a "Search for
 * patient" button. All searching happens in a nested PatientSearchDialog
 * (BackChart parity: read-only name box + a search popup). The parent passes
 * `initialLabel` so the chip shows a name when editing or pre-filling without
 * re-fetching.
 */
export function PatientPicker({
  value,
  initialLabel,
  onChange,
  disabled,
}: {
  value: string | null;
  initialLabel?: string | null;
  onChange: (patientId: string | null, patient: PatientListItemDto | null) => void;
  disabled?: boolean;
}) {
  const [selected, setSelected] = useState<PatientListItemDto | null>(null);
  const [label, setLabel] = useState<string | null>(initialLabel ?? null);
  const [searchOpen, setSearchOpen] = useState(false);

  useEffect(() => {
    if (value === null) {
      setSelected(null);
      setLabel(null);
    } else if (initialLabel) {
      setLabel(initialLabel);
    }
  }, [value, initialLabel]);

  const pick = (p: PatientListItemDto) => {
    setSelected(p);
    setLabel(patientLabel(p));
    setSearchOpen(false);
    onChange(p.id, p);
  };

  const clear = () => {
    setSelected(null);
    setLabel(null);
    onChange(null, null);
  };

  return (
    <div className="space-y-2">
      {value ? (
        <div className="flex items-center justify-between gap-2 rounded-md border border-[var(--color-border)] bg-[var(--color-muted)] px-3 py-2">
          <div className="flex min-w-0 items-center gap-2.5">
            <span
              aria-hidden
              className="grid h-7 w-7 shrink-0 place-items-center rounded-full bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
            >
              <UserRound className="h-3.5 w-3.5" />
            </span>
            <div className="min-w-0">
              <div className="truncate text-sm font-medium tracking-tight">{label ?? value}</div>
              {selected?.dateOfBirth && (
                <div className="truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
                  DOB {fmtDob(selected.dateOfBirth)}
                </div>
              )}
            </div>
          </div>
          {!disabled && (
            <div className="flex shrink-0 items-center gap-1">
              <Button type="button" variant="outline" size="sm" onClick={() => setSearchOpen(true)}>
                Change
              </Button>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={clear}
                aria-label="Clear patient"
              >
                <X className="h-3.5 w-3.5" />
                Clear
              </Button>
            </div>
          )}
        </div>
      ) : (
        <Button
          type="button"
          variant="outline"
          size="sm"
          className="w-full justify-start"
          onClick={() => setSearchOpen(true)}
          disabled={disabled}
        >
          <Search className="h-3.5 w-3.5" />
          Search for patient
        </Button>
      )}

      <PatientSearchDialog open={searchOpen} onOpenChange={setSearchOpen} onSelect={pick} />
    </div>
  );
}
```

- [ ] **Step 6: Pass `disabled={readOnly}` from the appointment dialog**

In `clients/dashboard/src/pages/scheduling/appointments.tsx`, find the `<PatientPicker …>` (around line 993) and add the `disabled` prop so a rescheduled (read-only) appointment can't have its patient swapped:

```tsx
                  <PatientPicker
                    value={patientId}
                    initialLabel={
                      editing?.patientId
                        ? editPatientLabel ?? "Loading patient…"
                        : creating?.patientLabel ?? null
                    }
                    onChange={(id, p) => {
                      setPatientId(id);
                      if (p && !notes) setNotes(patientLabel(p));
                    }}
                    disabled={readOnly}
                  />
```

- [ ] **Step 7: Run the tests to verify they pass**

Run: `cd clients/dashboard && npx playwright test appointments.spec`
Expected: PASS — all appointment tests green (the two edited/added tests plus the existing 9).

- [ ] **Step 8: Typecheck**

Run: `cd clients/dashboard && npx tsc -b --noEmit`
Expected: no output (clean). If TS6133 fires, remove any now-unused import.

- [ ] **Step 9: Commit**

```bash
git add clients/dashboard/src/components/scheduling/patient-search-dialog.tsx \
        clients/dashboard/src/components/scheduling/patient-picker.tsx \
        clients/dashboard/src/pages/scheduling/appointments.tsx \
        clients/dashboard/tests/scheduling/appointments.spec.ts
git commit -F- <<'EOF'
feat(scheduling): collapse patient picker + BackChart search popup

Move the patient typeahead into a nested PatientSearchDialog. A selected
patient now renders as a read-only name chip with Change/Clear; the empty
state is a "Search for patient" button. Both open the popup. Rescheduled
(read-only) appointments disable the control.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

### Task 2: Pre-fill the patient from the active patient tab

**Files:**
- Modify: `clients/dashboard/src/pages/scheduling/appointments.tsx`
- Test: `clients/dashboard/tests/scheduling/appointments.spec.ts`

**Interfaces:**
- Consumes: `useActivePatientTab()` from `@/state/patient-workspace-context` → returns `{ patientId: string; patientLabel: string; … } | null`. The create-dialog state shape already accepts optional `patientId?: string | null` and `patientLabel?: string | null`.
- Produces: nothing new — behavioral change only.

- [ ] **Step 1: Add the pre-fill test (failing)**

Add this test inside the `test.describe("scheduling/appointments", …)` block:

```typescript
  test("New appointment pre-fills the active patient tab", async ({ page }) => {
    const activePatientId = "00000000-0000-0000-0000-0000000000b2";
    // Seed the persistent workspace so a patient tab is "active" on load.
    await page.addInitScript(
      ([key, patientId]) => {
        window.sessionStorage.setItem(
          key,
          JSON.stringify({
            openTabs: [
              {
                patientId,
                patientLabel: "Roe, Rick",
                activeIncidentId: null,
                openReportIds: [],
                activeReportId: null,
              },
            ],
            activePatientId: patientId,
          }),
        );
      },
      ["fsh.dashboard.patientWorkspace.v1", activePatientId],
    );
    await mockScheduling(page);
    await page.goto("/scheduling/appointments");
    await page.getByRole("button", { name: /new appointment/i }).click();

    const dialog = page.getByRole("dialog");
    // Pre-filled: the active patient shows as a chip with Change — not the empty button.
    await expect(dialog.getByText("Roe, Rick")).toBeVisible();
    await expect(dialog.getByRole("button", { name: /^change$/i })).toBeVisible();
    await expect(dialog.getByRole("button", { name: /search for patient/i })).toHaveCount(0);
  });
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd clients/dashboard && npx playwright test appointments.spec --grep "pre-fills the active patient tab"`
Expected: FAIL — "Roe, Rick" is not present; the dialog still shows the empty "Search for patient" button.

- [ ] **Step 3: Import the active-tab hook**

In `clients/dashboard/src/pages/scheduling/appointments.tsx`, add the import near the other state imports (e.g. below the `useRealtimeEvent` import):

```tsx
import { useActivePatientTab } from "@/state/patient-workspace-context";
```

- [ ] **Step 4: Read the active tab in the page component**

Inside `AppointmentsPage()`, near the top (e.g. just after `const queryClient = useQueryClient();`), add:

```tsx
  const activePatientTab = useActivePatientTab();
```

- [ ] **Step 5: Seed the toolbar "New appointment" button**

Find the `EntityPageHeader` "New appointment" `<Button …>` (around line 484) and add the two patient fields to its `setDialog` call:

```tsx
          onClick={() =>
            setDialog({
              mode: "create",
              providerId: activeProviderIds[0] ?? providers[0]?.id ?? "",
              ymd: localYmd(date),
              startTime: "09:00",
              endTime: "09:30",
              patientId: activePatientTab?.patientId ?? null,
              patientLabel: activePatientTab?.patientLabel ?? null,
            })
          }
```

- [ ] **Step 6: Seed the calendar slot-select create**

Find `onSelectSlot={(slot) => { … }}` (around line 586) and add the two patient fields to its `setDialog` call:

```tsx
            onSelectSlot={(slot) => {
              const start = slot.start as Date;
              const end = slot.end as Date;
              setDialog({
                mode: "create",
                providerId: (slot as { resourceId?: string }).resourceId ?? activeProviderIds[0] ?? "",
                ymd: localYmd(start),
                startTime: `${pad(start.getHours())}:${pad(start.getMinutes())}`,
                endTime: `${pad(end.getHours())}:${pad(end.getMinutes())}`,
                patientId: activePatientTab?.patientId ?? null,
                patientLabel: activePatientTab?.patientLabel ?? null,
              });
            }}
```

- [ ] **Step 7: Run the test to verify it passes**

Run: `cd clients/dashboard && npx playwright test appointments.spec --grep "pre-fills the active patient tab"`
Expected: PASS.

- [ ] **Step 8: Run the full spec + typecheck (no regressions)**

Run: `cd clients/dashboard && npx playwright test appointments.spec && npx tsc -b --noEmit`
Expected: all appointment tests PASS; typecheck clean.

- [ ] **Step 9: Commit**

```bash
git add clients/dashboard/src/pages/scheduling/appointments.tsx \
        clients/dashboard/tests/scheduling/appointments.spec.ts
git commit -F- <<'EOF'
feat(scheduling): pre-fill new appointment with the active patient

New appointment (toolbar + calendar slot) seeds the patient field from the
active patient tab, so scheduling while a chart is open starts with that
patient already selected. The chart's Schedule button flow is unchanged.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Self-Review

**Spec coverage:**
- Goal 1 (auto pre-fill) → Task 2 (active tab) + existing chart router-state flow (unchanged). ✓
- Goal 2 (name label + Change, resting state whenever set) → Task 1, Step 5 (`value ?` chip branch). ✓
- Goal 3 (nested popup search on Change) → Task 1, Steps 4–5 (`PatientSearchDialog`, opened by Change/Search buttons). ✓
- Empty state = "Search for patient" button (approved deviation) → Task 1, Step 5 (`else` branch) + Step 1 test update. ✓
- `disabled={readOnly}` correctness fix → Task 1, Step 6. ✓

**Placeholder scan:** No TBD/TODO; every code step shows full code. ✓

**Type consistency:** `PatientSearchDialog` props (`open`, `onOpenChange`, `onSelect`) match usage in `PatientPicker` Step 5. `pick(p: PatientListItemDto)` matches `onSelect` signature. `patientLabel` stays exported and is still imported in `appointments.tsx`. `useActivePatientTab()` returns `OpenPatientTab | null` with `patientId`/`patientLabel` — matches Task 2 usage. ✓

**Note for implementer:** Task 1 and Task 2 both edit `appointments.tsx` and the same spec file — run Task 1 fully (commit) before Task 2 to avoid overlap.
