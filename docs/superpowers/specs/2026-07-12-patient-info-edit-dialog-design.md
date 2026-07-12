# Patient Info edit as a dialog (dashboard)

**Date:** 2026-07-12
**App:** `clients/dashboard`
**Branch:** `clinic-app`

## Problem

On the patient chart, the **Patient Info** card's Edit pencil
([`chart.tsx:451`](../../../clients/dashboard/src/pages/patient-charts/chart.tsx)) is a
`<Link to={/patients/:patientId}>` that navigates away to a full **`PatientDetailPage`**
(`patients/patient-detail.tsx`, ~1960 lines). We want editing to happen **in a dialog**
launched from the chart, with no navigation, and the chart's Patient Info card must reflect
any changes immediately after save.

This mirrors BackChart, where the dashboard's edit opens `PatientDemographicsDialog`, which
hosts the *entire* `PatientDetail` editor inside one modal.

## Decisions (confirmed with user)

1. **Dialog scope:** the dialog hosts the **full editor** — all 8 sections (Demographics,
   PHI, Flags, Contact, Next-of-kin, Employment, Guardian, Insurance), same capability as the
   page today.
2. **Old page + route:** **deleted**. `PatientDetailPage` and its `patients/:patientId` route
   are removed; nothing else links there. Deep links to `/patients/:id` will 404 (acceptable).
3. **Section edits:** **nested (stacked) dialogs** — the per-section edit dialogs are reused
   verbatim and open stacked on top of the container dialog. Radix handles stacking/focus;
   Esc closes the inner dialog first.

## Approach — extract, don't rewrite

`PatientDetailPage`'s body already *is* the full editor: read-only section panels, each with
an Edit button that opens a per-section dialog. We lift that into a reusable component and host
it in a modal. **No form/validation/mutation logic is rewritten.**

### New files

- **`clients/dashboard/src/pages/patients/patient-info-editor.tsx`**
  The extracted editor. Exports `PatientInfoEditor({ patient, onDeleted })`.
  Contains, moved out of `patient-detail.tsx` verbatim:
  - Read-only panels: `DemographicsPanel`, `PhiPanel`, `FlagsPanel`, `AuditPanel`,
    `ContactPanel`, `NextOfKinPanel`, `EmploymentPanel`, `GuardianPanel`, `InsurancePanel`.
  - Display/date helpers: `findOptionLabel`, `MetaRow`, `IdCode`, `EmptySection`, `FlagRow`,
    `fullName`, `calculateAge`, `toDateInputValue`, `fromDateInputValue`.
  - `useUpdateMutation` (with the cache fix below).
  - Per-section edit dialogs: `DemographicsDialog`, `ContactDialog`, `PhiDialog`,
    `EmploymentDialog`, `GuardianDialog`, `NextOfKinDialog`, `InsuranceDialog`, `FlagsDialog`.
  - Status/lifecycle dialogs: `ToggleStatusDialog`, `DeleteDialog`.
  - Internal `DialogState` state machine (same union as today) tracking which section/action
    dialog is open.
  - A compact top **action bar** inside the editor: the Active/Inactive status badge plus
    **Deactivate/Reactivate** and **Delete** buttons (the actions previously in `PatientHero`).
  - The **two-column panels grid** (aside: Demographics / PHI / Flags / Audit; main: Contact /
    Next-of-kin / Employment / Guardian [minor only] / Insurance), each panel's Edit button
    opening its dialog — unchanged from the page.

- **`clients/dashboard/src/pages/patients/patient-info-dialog.tsx`**
  The container modal. Exports `PatientInfoDialog({ patientId, open, onClose })`.
  - Loads the patient with `useQuery({ queryKey: ["patients", patientId], queryFn: () =>
    getPatientById(patientId) })` — **the same key the chart card uses**, so they share cache
    and a single invalidation refreshes both.
  - `<Dialog open onOpenChange>` → `<DialogContent className="!max-w-4xl">`.
  - `DialogHeader`: `DialogTitle` = patient full name; `DialogDescription` = `code · DOB (age)`.
  - `DialogBody` (scrollable, `max-h`): loading skeleton / not-found / `<PatientInfoEditor
    patient onDeleted={handleDeleted} />`.
  - `handleDeleted`: close the dialog and `navigate("/patient-charts")` (same destination the
    page used on delete). Close the workspace tab via `usePatientWorkspace().closePatient` **if
    that method exists** (verify during implementation; otherwise omit).

### Changed files

- **`clients/dashboard/src/pages/patient-charts/chart.tsx`**
  - Add `const [infoDialogOpen, setInfoDialogOpen] = useState(false);`.
  - Replace the `<Link to={/patients/:id}>` pencil (lines ~450–457) with a `<button
    onClick={() => setInfoDialogOpen(true)}>` carrying the same title/aria/classes and `<Pencil>`.
  - Render `{patientId && <PatientInfoDialog patientId={patientId} open={infoDialogOpen}
    onClose={() => setInfoDialogOpen(false)} />}` alongside the other chart dialogs.

- **`clients/dashboard/src/pages/patients/patient-detail.tsx`** — **deleted.**

- **`clients/dashboard/src/routes.tsx`**
  - Remove the `PatientDetailPage` lazy import (lines ~124–127) and the
    `{ path: "patients/:patientId", ... }` route (line ~238).

## Cache alignment (the "also updates the chart card" requirement)

Today `useUpdateMutation` invalidates `["patients","detail",patientId]` + `["patients","list"]`,
but the chart card reads `["patients", patientId]`, so edits never refresh the card.

**Fix:** `useUpdateMutation.onSuccess` invalidates the **`["patients"]` prefix**:
`queryClient.invalidateQueries({ queryKey: ["patients"] })`. This refreshes the card
(`["patients", patientId]`), the dialog's own query (same key), and the list
(`["patients","list"]`) in one call. `DeleteDialog` likewise invalidates `["patients"]`.
(Keys like `["patient-notes"]` / `["problems"]` have a different first element and are
unaffected.)

## What stays identical

All field-level forms, FluentValidation-mirroring client validation, `mergePatientUpdate`
full-replace PUT semantics, PHI masking (blank = keep encrypted value), the minor→guardian
conditional section, toast messages, and the delete/deactivate flows.

## Out of scope

- No backend changes.
- The elaborate `PatientHero` stats/meta strip (age tile, last/next-visit tiles, email/phone
  meta) is **not** reproduced in the dialog — those live on the chart card already. Only the
  hero's *actions* (status badge, Deactivate, Delete) carry over, into the editor's action bar.

## Testing

- Playwright (route-mocked, dashboard app): from a patient chart, clicking the Patient Info
  Edit pencil opens the dialog (no navigation); editing a section (e.g. Demographics name)
  and saving closes the section dialog, and the chart's Patient Info card reflects the new
  name without a page reload. Deleting closes the dialog and returns to `/patient-charts`.
- Manual: nested dialog stacking (open a section dialog over the container; Esc closes the
  section dialog only), minor→guardian section visibility.

## Docs / changelog

Per repo golden rule #10, add a changelog entry in the separate docs repo
(`src/content/docs/changelog/`) noting the dashboard patient-info edit moved from a page to a
dialog. (Deferred, tracked — consistent with prior sprints in this branch.)
