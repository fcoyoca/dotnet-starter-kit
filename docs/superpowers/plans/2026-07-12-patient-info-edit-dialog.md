# Patient Info Edit Dialog Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the chart's "Edit patient info" page navigation with an in-place dialog that hosts the full patient editor, so edits reflect on the chart's Patient Info card immediately.

**Architecture:** Extract `PatientDetailPage`'s editor body (read-only section panels + per-section edit dialogs + status/delete dialogs) into a reusable `PatientInfoEditor` component. Host it in a new `PatientInfoDialog` modal opened from the chart. Align cache invalidation so section saves refresh the chart card. Delete the now-orphaned page and route.

**Tech Stack:** React 19, TypeScript, TanStack Query v5, Radix Dialog (shadcn-style `@/components/ui/dialog`), React Router 7, Playwright (route-mocked).

## Global Constraints

- App: `clients/dashboard`. All paths below are relative to `clients/dashboard/`.
- Build gate: `npm run build` (runs `tsc -b` + Vite) must pass. `TreatWarningsAsErrors`-style strictness — unused imports fail the build.
- Frontend rule: pass per-call data through `mutate(arg)`, never via closed-over state.
- Patient update is a **single full-replace PUT** — every section dialog builds its payload via `mergePatientUpdate(patient, { …changedFields })`. Do not change this.
- Query keys: the chart card and the new dialog both read the patient under `["patients", patientId]`. Section mutations must invalidate the **`["patients"]` prefix**.
- Run Playwright from `clients/dashboard/`: `npx playwright test <path>`.

---

### Task 1: Extract `PatientInfoEditor` (and align cache invalidation)

Move the editor body out of `patient-detail.tsx` into a new reusable component, add an internal action bar (status badge + Deactivate/Reactivate + Delete), and fix the cache-invalidation keys. `patient-detail.tsx` is temporarily refactored into a thin page that consumes the new component, so the existing `tests/patients/patients.spec.ts` stays green and proves the extraction preserved behavior.

**Files:**
- Create: `src/pages/patients/patient-info-editor.tsx`
- Modify: `src/pages/patients/patient-detail.tsx` (becomes a thin wrapper)
- Test: `tests/patients/patients.spec.ts` (existing — run unchanged as the regression gate)

**Interfaces:**
- Produces: `export function PatientInfoEditor({ patient, onDeleted }: { patient: PatientDetailDto; onDeleted: () => void }): JSX.Element`
- Consumes: existing `getPatientById`/`updatePatient`/`deletePatient` from `@/api/patients`, `mergePatientUpdate` from `@/pages/patients/patient-mappers`, lookups from `@/lib/patient-lookups`, UI from `@/components/ui/*` and `@/components/list`.

- [ ] **Step 1: Create `patient-info-editor.tsx` by moving code verbatim from `patient-detail.tsx`.**

Move these identifiers **unchanged** out of `patient-detail.tsx` into the new file (they are currently at the cited line ranges):
  - Date/name helpers: `fullName` (90–93), `calculateAge` (95–103), `toDateInputValue` (105–108), `fromDateInputValue` (110–112).
  - Read-only panels + display helpers: `DemographicsPanel`, `PhiPanel`, `FlagsPanel`, `AuditPanel`, `ContactPanel`, `NextOfKinPanel`, `EmploymentPanel`, `GuardianPanel`, `InsurancePanel`, `findOptionLabel`, `MetaRow`, `IdCode`, `EmptySection` (498–758).
  - `useUpdateMutation` (823–835).
  - Edit dialogs: `DemographicsDialog`, `ContactDialog`, `PhiDialog`, `EmploymentDialog`, `GuardianDialog`, `NextOfKinDialog`, `InsuranceDialog`, `FlagsDialog`, `FlagRow`, `ToggleStatusDialog`, `DeleteDialog` (837–1960).

Bring the corresponding imports across (from `react`, `@tanstack/react-query`, `lucide-react`, `sonner`, `@/api/patients`, `@/pages/patients/patient-mappers`, `@/lib/patient-lookups`, `@/components/ui/button|input|skeleton|switch|dialog`, `@/components/list`, `@/lib/cn`, `@/lib/list-helpers`). Keep only the icons actually used by the moved code (`Banknote, Briefcase, ClipboardList, IdCard, Info, Lock, MapPin, Pencil, Power, PowerOff, UserSquare2, Users, Trash2`).

- [ ] **Step 2: Apply the cache-invalidation fix inside `patient-info-editor.tsx`.**

In `useUpdateMutation`, change the two invalidations to a single prefix invalidation:

```tsx
function useUpdateMutation(patient: PatientDetailDto, onClose: () => void, successMessage: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: UpdatePatientInput) => updatePatient(input),
    onSuccess: () => {
      toast.success(successMessage);
      // Prefix invalidation refreshes the chart's Patient Info card
      // (["patients", patientId]), the dialog's own query (same key), and
      // the list (["patients","list"]) in one shot.
      void queryClient.invalidateQueries({ queryKey: ["patients"] });
      onClose();
    },
    onError: (err: unknown) => toast.error("Update failed", { description: describe(err) }),
  });
}
```

In `DeleteDialog`'s mutation `onSuccess`, change `invalidateQueries({ queryKey: ["patients", "list"] })` to `invalidateQueries({ queryKey: ["patients"] })`.

- [ ] **Step 3: Add the `PatientInfoEditor` component (action bar + panels grid) at the top of `patient-info-editor.tsx`.**

This reproduces the page's two-column panels grid (from `patient-detail.tsx` lines 152–362) plus a compact action bar carrying the hero's Deactivate/Delete actions and status badge. It owns the `DialogState` machine.

```tsx
type DialogState =
  | { mode: "closed" }
  | { mode: "edit-demographics" }
  | { mode: "edit-contact" }
  | { mode: "edit-phi" }
  | { mode: "edit-employment" }
  | { mode: "edit-guardian" }
  | { mode: "edit-next-of-kin" }
  | { mode: "edit-insurance" }
  | { mode: "edit-flags" }
  | { mode: "toggle-status" }
  | { mode: "delete" };

export function PatientInfoEditor({
  patient,
  onDeleted,
}: {
  patient: PatientDetailDto;
  onDeleted: () => void;
}) {
  const [dialog, setDialog] = useState<DialogState>({ mode: "closed" });

  return (
    <div className="space-y-5">
      {/* Action bar — status + lifecycle actions (previously the page hero's actions) */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {patient.isActive ? (
            <EntityStatusBadge tone="success">Active</EntityStatusBadge>
          ) : (
            <EntityStatusBadge tone="default">Inactive</EntityStatusBadge>
          )}
          {patient.demographics.isMinor && <EntityStatusBadge tone="info">Minor</EntityStatusBadge>}
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={() => setDialog({ mode: "toggle-status" })} className="gap-1.5">
            {patient.isActive ? <PowerOff className="h-3.5 w-3.5" /> : <Power className="h-3.5 w-3.5" />}
            <span className="hidden sm:inline">{patient.isActive ? "Deactivate" : "Reactivate"}</span>
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setDialog({ mode: "delete" })}
            className="gap-1.5 hover:!border-[var(--color-destructive)] hover:!text-[var(--color-destructive)]"
          >
            <Trash2 className="h-3.5 w-3.5" />
            <span className="hidden sm:inline">Delete</span>
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-5 lg:grid-cols-[300px_1fr]">
        {/* Left: demographics + phi + flags + audit */}
        <aside className="space-y-5">
          <EntityDetailSection
            title="Demographics"
            icon={IdCard}
            action={<EditButton onClick={() => setDialog({ mode: "edit-demographics" })} />}
          >
            <DemographicsPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Protected health info"
            icon={Lock}
            description="SSN is encrypted at rest and only ever shown masked."
            action={<EditButton onClick={() => setDialog({ mode: "edit-phi" })} />}
          >
            <PhiPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Flags &amp; visits"
            icon={ClipboardList}
            action={<EditButton onClick={() => setDialog({ mode: "edit-flags" })} />}
          >
            <FlagsPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection title="Audit" icon={Info}>
            <AuditPanel patient={patient} />
          </EntityDetailSection>
        </aside>

        {/* Right: contact + kin + employment + guardian + insurance */}
        <div className="space-y-5">
          <EntityDetailSection
            title="Contact"
            icon={MapPin}
            action={<EditButton onClick={() => setDialog({ mode: "edit-contact" })} />}
          >
            <ContactPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Next of kin"
            icon={Users}
            action={<EditButton onClick={() => setDialog({ mode: "edit-next-of-kin" })} />}
          >
            <NextOfKinPanel patient={patient} />
          </EntityDetailSection>
          <EntityDetailSection
            title="Employment"
            icon={Briefcase}
            action={<EditButton onClick={() => setDialog({ mode: "edit-employment" })} />}
          >
            <EmploymentPanel patient={patient} />
          </EntityDetailSection>
          {patient.demographics.isMinor && (
            <EntityDetailSection
              title="Guardian"
              icon={UserSquare2}
              description="Required while this patient is recorded as a minor."
              action={<EditButton onClick={() => setDialog({ mode: "edit-guardian" })} />}
            >
              <GuardianPanel patient={patient} />
            </EntityDetailSection>
          )}
          <EntityDetailSection
            title="Insurance"
            icon={Banknote}
            action={<EditButton onClick={() => setDialog({ mode: "edit-insurance" })} />}
          >
            <InsurancePanel patient={patient} />
          </EntityDetailSection>
        </div>
      </div>

      {/* Nested edit dialogs (stack on top of the container dialog) */}
      <DemographicsDialog open={dialog.mode === "edit-demographics"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <ContactDialog open={dialog.mode === "edit-contact"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <PhiDialog open={dialog.mode === "edit-phi"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <EmploymentDialog open={dialog.mode === "edit-employment"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <GuardianDialog open={dialog.mode === "edit-guardian"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <NextOfKinDialog open={dialog.mode === "edit-next-of-kin"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <InsuranceDialog open={dialog.mode === "edit-insurance"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <FlagsDialog open={dialog.mode === "edit-flags"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <ToggleStatusDialog open={dialog.mode === "toggle-status"} patient={patient} onClose={() => setDialog({ mode: "closed" })} />
      <DeleteDialog
        open={dialog.mode === "delete"}
        patient={patient}
        onClose={() => setDialog({ mode: "closed" })}
        onDeleted={onDeleted}
      />
    </div>
  );
}

function EditButton({ onClick }: { onClick: () => void }) {
  return (
    <Button variant="outline" size="sm" onClick={onClick} className="gap-1.5">
      <Pencil className="h-3.5 w-3.5" />
      Edit
    </Button>
  );
}
```

Add to the file's imports: `useState` from `react`; `EntityDetailSection`, `EntityStatusBadge` from `@/components/list`; icons `Banknote, Briefcase, ClipboardList, IdCard, Info, Lock, MapPin, Pencil, Power, PowerOff, Trash2, Users, UserSquare2` from `lucide-react`.

- [ ] **Step 4: Refactor `patient-detail.tsx` into a thin page consuming the editor.**

Replace the whole file body with a slim page that keeps the back link + patient name heading (so `patients.spec.ts` assertions still pass) and renders `PatientInfoEditor`. It keeps its own `getPatientById` query under the page-local key.

```tsx
import { Link, useNavigate, useParams } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { Stethoscope } from "lucide-react";
import { getPatientById } from "@/api/patients";
import { Button } from "@/components/ui/button";
import { EntityDetailBack, ErrorBand } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { PatientInfoEditor } from "@/pages/patients/patient-info-editor";

export function PatientDetailPage() {
  const { patientId = "" } = useParams<{ patientId: string }>();
  const navigate = useNavigate();

  const patientQuery = useQuery({
    queryKey: ["patients", "detail", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: !!patientId,
  });
  const patient = patientQuery.data;

  return (
    <div className="pb-12">
      <EntityDetailBack to={`/patient-charts/${patientId}`} label="Back to chart" />
      {patientQuery.isError && (
        <div className="mb-5">
          <ErrorBand message={describe(patientQuery.error)} />
        </div>
      )}
      {patientQuery.isLoading ? (
        <div className="skeleton h-96 rounded-xl" />
      ) : patient ? (
        <>
          <h1 className="mb-5 text-[22px] font-semibold">
            {[patient.demographics.firstName, patient.demographics.middleInitial, patient.demographics.lastName]
              .filter(Boolean)
              .join(" ")}
          </h1>
          <PatientInfoEditor patient={patient} onDeleted={() => navigate("/patient-charts")} />
        </>
      ) : (
        <div className="flex flex-col items-center justify-center rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] px-8 py-16 text-center">
          <Stethoscope className="mb-4 size-7 text-[var(--color-primary)]" />
          <h3 className="mb-1.5 text-[17px] font-semibold">Patient not found</h3>
          <p className="mb-6 text-[13px] text-[var(--color-muted-foreground)]">
            It may have been deleted, or the link may be wrong.
          </p>
          <Button asChild variant="outline" size="sm">
            <Link to="/patient-charts">Back to Patient Chart</Link>
          </Button>
        </div>
      )}
    </div>
  );
}
```

> Note: this thin page is transient — Task 4 deletes it. It exists only to keep `patients.spec.ts` green while entry points are rewired.

- [ ] **Step 5: Build.**

Run: `npm run build`
Expected: PASS (no TS/unused-import errors).

- [ ] **Step 6: Run the existing patients suite (unchanged) as the regression gate.**

Run: `npx playwright test tests/patients/patients.spec.ts`
Expected: PASS — all detail-render, minor/guardian, not-found, next-of-kin coupling, and edit-section tests still pass (the page still lives at `/patients/:id` and its dialogs behave identically).

- [ ] **Step 7: Commit.**

```bash
git add src/pages/patients/patient-info-editor.tsx src/pages/patients/patient-detail.tsx
git commit -m "refactor(patients): extract PatientInfoEditor; invalidate [\"patients\"] on save"
```

---

### Task 2: Add `PatientInfoDialog` container

A modal that loads the patient under the chart's key (`["patients", patientId]`) and hosts `PatientInfoEditor`.

**Files:**
- Create: `src/pages/patients/patient-info-dialog.tsx`

**Interfaces:**
- Consumes: `PatientInfoEditor` from Task 1; `getPatientById` from `@/api/patients`.
- Produces: `export function PatientInfoDialog({ patientId, open, onClose }: { patientId: string; open: boolean; onClose: () => void }): JSX.Element`

- [ ] **Step 1: Create the container.**

```tsx
import { useNavigate } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { getPatientById } from "@/api/patients";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { usePatientWorkspace } from "@/state/patient-workspace-context";
import { PatientInfoEditor } from "@/pages/patients/patient-info-editor";
import { formatDate } from "@/lib/list-helpers";

function ageOf(dob: string): number {
  const d = new Date(dob);
  if (Number.isNaN(d.getTime())) return 0;
  const now = new Date();
  let age = now.getFullYear() - d.getFullYear();
  const m = now.getMonth() - d.getMonth();
  if (m < 0 || (m === 0 && now.getDate() < d.getDate())) age--;
  return age;
}

export function PatientInfoDialog({
  patientId,
  open,
  onClose,
}: {
  patientId: string;
  open: boolean;
  onClose: () => void;
}) {
  const navigate = useNavigate();
  const { closePatient } = usePatientWorkspace();

  // Same key the chart card reads → shared cache, single invalidation refreshes both.
  const patientQuery = useQuery({
    queryKey: ["patients", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: open && !!patientId,
  });
  const patient = patientQuery.data;

  const handleDeleted = () => {
    onClose();
    closePatient(patientId);
    navigate("/patient-charts");
  };

  const title = patient
    ? [patient.demographics.firstName, patient.demographics.middleInitial, patient.demographics.lastName]
        .filter(Boolean)
        .join(" ")
    : "Patient Info";

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-4xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {patient && (
            <DialogDescription>
              {patient.patientCode} · {formatDate(patient.demographics.dateOfBirth)} (
              {ageOf(patient.demographics.dateOfBirth)} yrs)
            </DialogDescription>
          )}
        </DialogHeader>
        <DialogBody className="max-h-[70vh] overflow-y-auto">
          {patientQuery.isLoading ? (
            <div className="skeleton h-96 rounded-xl" />
          ) : patient ? (
            <PatientInfoEditor patient={patient} onDeleted={handleDeleted} />
          ) : (
            <p className="py-12 text-center text-[13px] text-[var(--color-muted-foreground)]">
              Patient not found.
            </p>
          )}
        </DialogBody>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 2: Build.**

Run: `npm run build`
Expected: PASS. (Component compiles though not yet imported — Vite tree-shakes; `tsc -b` still type-checks it.)

- [ ] **Step 3: Commit.**

```bash
git add src/pages/patients/patient-info-dialog.tsx
git commit -m "feat(patients): add PatientInfoDialog container modal"
```

---

### Task 3: Wire the chart's Edit pencil to open the dialog

Replace the `<Link to={/patients/:id}>` pencil with a button that opens `PatientInfoDialog`, and update the chart test that asserted the old link.

**Files:**
- Modify: `src/pages/patient-charts/chart.tsx` (import + state + button at ~450–457 + render dialog near the other dialogs ~874+)
- Test: `tests/patient-charts/problems.spec.ts` (update link→button assertion; add card-refresh test)

**Interfaces:**
- Consumes: `PatientInfoDialog` from Task 2.

- [ ] **Step 1: Update the chart-test assertion first (red).**

In `tests/patient-charts/problems.spec.ts`, replace the link assertion (currently lines ~87–91):

```tsx
    // Patient Info card exposes an Edit button (opens the info dialog).
    await expect(page.getByRole("button", { name: /edit patient info/i })).toBeVisible();
```

- [ ] **Step 2: Run it to verify it fails.**

Run: `npx playwright test tests/patient-charts/problems.spec.ts -g "medical-alert banner"`
Expected: FAIL — the chart still renders a `link`, not a `button`, named "Edit patient info".

- [ ] **Step 3: Wire the chart.**

In `src/pages/patient-charts/chart.tsx`:

Add the import near the other page-dialog imports:
```tsx
import { PatientInfoDialog } from "@/pages/patients/patient-info-dialog";
```

Add state alongside the other `useState` flags (near line 213):
```tsx
  const [infoDialogOpen, setInfoDialogOpen] = useState(false);
```

Replace the `<Link>` pencil (lines ~450–457) with a button carrying the same title/aria/classes:
```tsx
                  <button
                    type="button"
                    title="Edit patient info"
                    aria-label="Edit patient info"
                    onClick={() => setInfoDialogOpen(true)}
                    className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] text-[var(--color-foreground)] transition-colors hover:bg-[var(--color-accent)]"
                  >
                    <Pencil className="size-4" />
                  </button>
```

Render the dialog alongside the other chart dialogs (e.g. just after the Create incident dialog block near line 881):
```tsx
      {/* Patient Info edit dialog (replaces the old /patients/:id page) */}
      {patientId && (
        <PatientInfoDialog
          patientId={patientId}
          open={infoDialogOpen}
          onClose={() => setInfoDialogOpen(false)}
        />
      )}
```

Remove the now-unused `Link` import **only if** no other `Link` usage remains in `chart.tsx` (there is one at the top "Patient Chart" back link and the tab strip — keep `Link` imported; verify with a search before removing).

- [ ] **Step 4: Build.**

Run: `npm run build`
Expected: PASS.

- [ ] **Step 5: Run the updated assertion to verify it passes.**

Run: `npx playwright test tests/patient-charts/problems.spec.ts`
Expected: PASS.

- [ ] **Step 6: Add a card-refresh test (the core requirement).**

Append to `tests/patient-charts/problems.spec.ts` a new test in the existing `test.describe("patient problem list", …)` block. It opens the dialog from the chart, edits the demographics first name, and asserts the chart card updates after save. The post-save refetch must return updated data, so re-register the GET mock with the new name right before saving.

```tsx
  test("editing demographics in the info dialog updates the chart card", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/problems**", paged([]));
    await page.goto(`/patient-charts/${PATIENT_ID}`);

    // Card shows the current name.
    await expect(page.getByText("Alice Q Vance")).toBeVisible();

    // Open the info dialog, then the Demographics section dialog (nested).
    await page.getByRole("button", { name: /edit patient info/i }).click();
    const infoDialog = page.getByRole("dialog").filter({ hasText: "Demographics" });
    await infoDialog
      .locator("section", { has: page.getByRole("heading", { name: "Demographics" }) })
      .getByRole("button", { name: /edit/i })
      .click();

    const editDialog = page.getByRole("dialog").filter({ hasText: "Edit demographics" });
    await editDialog.getByLabel("First name").fill("Alicia");

    // After the PUT succeeds, the invalidated GET refetch must return the new name.
    const UPDATED = { ...PATIENT, demographics: { ...PATIENT.demographics, firstName: "Alicia" } };
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ID}`, '""', { method: "PUT" });
    await mockJsonResponse(page, `**/api/v1/patient/patients/${PATIENT_ID}`, UPDATED);

    await editDialog.getByRole("button", { name: /save changes/i }).click();

    // Chart card reflects the change without a navigation/reload.
    await expect(page.getByText("Alicia Q Vance")).toBeVisible();
  });
```

The `PATIENT` fixture at the top of this file needs the demographics fields the Demographics dialog reads; extend it so the edit dialog can render:
```tsx
const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: {
    firstName: "Alice",
    middleInitial: "Q",
    lastName: "Vance",
    dateOfBirth: "1990-04-12",
    gender: "F",
    maritalStatus: null,
    isMinor: false,
    raceId: null,
    ethnicityId: null,
    languageId: null,
    smokingStatusId: null,
    smokingStartDate: null,
    smokingEndDate: null,
    medicalAlertNotes: null,
  },
  contact: {
    address1: null, address2: null, city: null, state: null, zipCode: null,
    phone: null, phoneExtension: null, cellPhone: null, email: null, preferredContactMethodId: null,
  },
  phi: { ssnMasked: null },
  employment: null,
  guardian: null,
  nextOfKin: null,
  insurance: null,
  hasNoKnownProblems: false,
  hasNoKnownMedications: false,
  hasNoKnownAllergies: false,
  receivesEmailReminders: false,
  createdAtUtc: "2026-01-10T10:00:00Z",
  updatedAtUtc: null,
  lastVisitDate: null,
  nextVisitDate: null,
};
```

Also add the administration lookups the demographics/contact dialogs query, into this file's `beforeEach` (races/ethnicities/languages/smoking-statuses/preferred-contact-methods/referral-types) — mirror `mockAdministrationLookups` from `tests/patients/patients.spec.ts`, which returns **flat arrays** (not `paged(...)`) for these endpoints:
```tsx
    await mockJsonResponse(page, "**/api/v1/administration/races**", []);
    await mockJsonResponse(page, "**/api/v1/administration/ethnicities**", []);
    await mockJsonResponse(page, "**/api/v1/administration/languages**", []);
    await mockJsonResponse(page, "**/api/v1/administration/smoking-statuses**", []);
    await mockJsonResponse(page, "**/api/v1/administration/preferred-contact-methods**", []);
    await mockJsonResponse(page, "**/api/v1/administration/referral-types**", []);
```

- [ ] **Step 7: Run the new card-refresh test.**

Run: `npx playwright test tests/patient-charts/problems.spec.ts -g "updates the chart card"`
Expected: PASS. If the nested-dialog `filter({ hasText })` is ambiguous (two dialogs both contain "Demographics"), scope the section click via `page.getByRole("dialog").last()` for the inner dialog — adjust and re-run until green.

- [ ] **Step 8: Commit.**

```bash
git add src/pages/patient-charts/chart.tsx tests/patient-charts/problems.spec.ts
git commit -m "feat(chart): open Patient Info editor in a dialog; refresh card on save"
```

---

### Task 4: Delete the old page + route; migrate the patients suite to the dialog

Remove the orphaned page and its route, and rewrite `tests/patients/patients.spec.ts` to drive the editor through the chart dialog instead of the deleted `/patients/:id` page.

**Files:**
- Delete: `src/pages/patients/patient-detail.tsx`
- Modify: `src/routes.tsx` (remove lazy import ~124–127 and route ~238)
- Test: `tests/patients/patients.spec.ts` (rewrite navigation)

- [ ] **Step 1: Remove the route + lazy import in `src/routes.tsx`.**

Delete the `PatientDetailPage` lazy definition:
```tsx
const PatientDetailPage = lazyNamed(
  () => import("@/pages/patients/patient-detail"),
  "PatientDetailPage",
);
```
And delete the route line:
```tsx
          { path: "patients/:patientId", element: withSuspense(<PatientDetailPage />) },
```

- [ ] **Step 2: Delete the page file.**

```bash
git rm src/pages/patients/patient-detail.tsx
```

- [ ] **Step 3: Rewrite `tests/patients/patients.spec.ts` to reach the editor via the chart dialog.**

The fixtures (`PATIENT_ADULT`, `PATIENT_MINOR`, `PATIENT_NO_KIN`, lookups) stay. Changes:
  - Each test now needs the chart's extra mocks. Add to every `beforeEach`:
    ```tsx
    await mockJsonResponse(page, "**/api/v1/patient/incidents**", { items: [], totalCount: 0, page: 1, pageSize: 100 });
    await mockJsonResponse(page, "**/api/v1/administration/departments**", { items: [], totalCount: 0, page: 1, pageSize: 100 });
    await mockJsonResponse(page, "**/api/v1/administration/incident-types**", { items: [], totalCount: 0, page: 1, pageSize: 100 });
    ```
  - Replace `await page.goto(`/patients/${ID}`)` with a helper that opens the chart then the dialog:
    ```tsx
    async function openInfoDialog(page: import("@playwright/test").Page, id: string) {
      await page.goto(`/patient-charts/${id}`);
      await page.getByRole("button", { name: /edit patient info/i }).click();
      return page.getByRole("dialog").filter({ hasText: "Demographics" });
    }
    ```
  - "loads an adult patient" test: assert inside the dialog — patient name in the `DialogTitle`, Next of kin section shows "Spouse"/"SPS", no Guardian heading. Drop the "back to chart link" assertion (no longer applicable); assert the dialog is visible instead.
  - "loads a minor patient": open dialog, assert the "Minor" badge and Guardian section render inside it.
  - "not-found" test: the dialog's body shows "Patient not found." after the GET returns null — open the dialog and assert that text (the button still renders on the chart because the chart's own patient GET is mocked separately; give the chart a valid patient and the dialog GET null — or, simplest, assert the chart card's own not-found "Patient not found." path). Prefer: keep one not-found test at the chart-card level and delete the page-specific one if it no longer maps cleanly.
  - Next-of-kin coupling + edit-section tests: replace `page.goto` with `openInfoDialog`, then interact with the nested section dialog exactly as before (the section dialogs are unchanged). PUT capture via `page.waitForRequest` is unchanged.

Rename the top-of-file comment and `test.describe` titles from `patients/:patientId — …` to `patient info dialog — …` to reflect the new entry point.

- [ ] **Step 4: Build.**

Run: `npm run build`
Expected: PASS (no dangling import of the deleted page).

- [ ] **Step 5: Run the migrated patients suite + the chart suite.**

Run: `npx playwright test tests/patients/patients.spec.ts tests/patient-charts/problems.spec.ts`
Expected: PASS.

- [ ] **Step 6: Full dashboard E2E sanity (catch any other referencer).**

Run: `npx playwright test`
Expected: PASS. If a spec still navigates to `/patients/:id`, migrate it the same way.

- [ ] **Step 7: Commit.**

```bash
git add src/routes.tsx tests/patients/patients.spec.ts
git commit -m "refactor(patients): delete /patients/:id page+route; drive editor via chart dialog"
```

---

### Task 5: Docs / changelog

Per repo golden rule #10, a user-facing change updates the separate docs repo.

**Files:**
- (separate docs repo `github.com/fullstackhero/docs`) `src/content/docs/changelog/`

- [ ] **Step 1: Add a changelog entry** noting the dashboard patient-info edit moved from a standalone page to an in-chart dialog, with the chart card updating live on save.

- [ ] **Step 2:** If the docs repo is not checked out locally, record this as deferred in the plan's follow-ups (consistent with prior sprints on this branch) and note it in the final summary to the user.

---

## Notes for the implementer

- **Nested dialogs:** the section edit dialogs are Radix `Dialog`s opening over the container `Dialog`. Radix stacks modals and routes Esc to the top-most. No code needed beyond rendering them inside the editor.
- **Why `["patients"]` prefix:** the chart card query is `["patients", patientId]`; the detail/list queries are `["patients","detail",…]`/`["patients","list"]`. Invalidating the `["patients"]` prefix covers all three. Unrelated keys (`["patient-notes"]`, `["problems"]`, `["incidents"]`, `["reports"]`) have different first elements and are untouched.
- **`closePatient`** exists on `usePatientWorkspace()` (`src/state/patient-workspace-context.tsx`) — used on delete so the workspace tab for a deleted patient is removed.
