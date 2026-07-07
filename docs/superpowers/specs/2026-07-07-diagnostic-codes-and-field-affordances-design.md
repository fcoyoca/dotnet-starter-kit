# Diagnostic Codes Dialog & Report Field Affordances — Design

**Date:** 2026-07-07 · **Branch:** `clinic-app` · **Status:** Approved

Refinements to the shipped Procedures Performed feature, from user review:

1. **"Edit Dx Codes" must open BackChart's real *Diagnostic Codes* dialog** (category browse +
   DX search + add/remove incident dx), not the incident-edit dialog.
2. **The Procedures Performed button belongs on the "Plan" report section**, not the report
   header — legacy renders it per-field. Verified in `VisualReportIndex.razor`: field
   `rfID 24 = "Plan"` only. While touching that affordance row, also port the other two
   per-field actions (user approved): **Import Allergies** (field 5) and **Import
   Medications** (field 6).

Legacy references (verified against source + live LocalDB `Bronston`/`BronstonAuthenticatingDB`):

- `BackChart-FE/.../DiagnosticCodes/DiagnosticCodeDialog.razor` — the dialog
- `BackChart-FE/.../VisualReports/VisualReportIndex.razor(.cs)` — per-field links (Import Dx
  Codes on 15/22, Import Allergies on 5, Import Medications on 6, Procedures Performed on 24)
- `BackChart-FE/.../VisualReports/ImportAllergies/ImportAllergiesIndex.razor`,
  `ImportMedications/ImportMedicationIndex.razor` — the two select dialogs
- DB: `lupDiagnosticCategories` (33 rows, tenant), `ascDiagnosticCategories` (779 rows —
  many-to-many category↔dx-code; 776 reference the GLOBAL `lupDiagnostics` int ids, 3
  reference negative CustomDiagnostics ids), `viewDiagnosticCodes` (global ∪ custom),
  `Diagnostics_GetByCategoryID` proc (join through `ascDiagnosticCategories`)

## 1. Diagnostic Codes dialog

### Legacy behavior to port

- Header **"Diagnostic Codes"**. Left panel has two modes toggled by a **DX Search** button:
  - *Category mode* (default): **DX Categories** dropdown → codes of that category.
  - *Search mode*: text box (min 3 chars) + search button + **"Hide non-chiropractic codes"**
    checkbox (default ON).
- Right panel: results table (Code, Description); **double-click a DX to add it to this
  incident**; duplicate → warning "Item already in the list." Each add asks
  **"Would you like to add this DX Code to the Problem List for {patient}?"** (Yes → a
  patient problem is created on Save).
- Bottom: the incident's **DX Codes** table; **double-click a DX to remove from this
  incident** (plus a remove button in our port).
- Footer: **Save** (persists the whole set) / Close. Nothing persists before Save.

### clinic-app mapping

Incidents store `CustomDiagnostic` guids; category associations and DX search operate on the
**global** `Diagnostic` catalog (int ids) — matching legacy's data shape. Bridging:

- **New Administration entity `DiagnosticCategoryCode`** (`DiagnosticCategoryId` Guid ↔
  `DiagnosticId` int, composite PK, tenant-scoped like `PatientIncidentDiagnostic`) + one
  migration. Legacy's 3 negative (custom) refs are skipped at data-migration time with a log.
- **`categoryId` filter** on the existing global `ListDiagnosticsQuery` (join through the new
  table).
- **Association management**: `GET /administration/diagnostic-categories/{id}/codes` (list
  with code/description) + `PUT .../codes` replace-set (exact `SetInsuranceTypeProcedures`
  pattern), gated on the existing DiagnosticCategories permissions. Admin UI: a "Codes"
  editor on the existing Diagnostic Categories page (search global codes, add/remove,
  save) — mirrors the insurance-type procedures editor.
- **DbMigrator**: extend the lookup migration with `ascDiagnosticCategories` → join rows
  (category by `DiagnosticCategory.LegacyId`, code by `Diagnostic.LegacyId`; idempotent
  delete-and-reinsert like sibling runners; skip+log unmatched/negative).
- **`POST /administration/custom-diagnostics/ensure`** — find-or-create the tenant
  `CustomDiagnostic` for a picked global code (match by Code, case-insensitive, incl.
  soft-deleted → reactivate; else create with the global code's fields). Returns the guid.
  Needed because incident dx are CustomDiagnostic guids. Gated on CustomDiagnostics.Create.
- **`PUT /patient/incidents/{id}/diagnostics`** — `SetIncidentDiagnosticsCommand
  (IncidentId, IReadOnlyList<Guid> DiagnosticIds)` replace-set slice (SetReportProblems
  pattern), gated on `Incidents.Update`. The dialog saves through this, not the full
  incident PUT.
- **Frontend `diagnostic-codes-dialog.tsx`**: legacy layout/behavior above. On Save:
  `ensure` each newly-added global code → guids, `PUT` the full set, then create patient
  problems for adds the user confirmed (via existing `createProblem`; global dx have the
  int id + code it needs), invalidate `["incident", incidentId]`. Divergence: the bottom
  table shows Code + Description (no Category column — custom snapshots carry none).
- **Procedures Performed dialog**: "Edit Dx Codes" opens this dialog (replaces the
  IncidentDialog usage). The incident-edit dialog elsewhere is untouched.

## 2. Report editor field affordances

Remove the header-strip Procedures Performed button. In the per-field affordance row
(where Import Dx Codes / Macros already render), matched by field name like
`DX_IMPORT_FIELDS` (fields are per-type copies):

| Field name match | Affordance | Behavior |
|---|---|---|
| `plan` (PLAN_FIELDS set) | **Procedures Performed** | opens the ProceduresPerformedDialog for this report; macro text inserts into THIS field. Rendered even when the report is signed/read-only (super bill stays editable post-sign — legacy parity); Macros/Imports stay draft-only. |
| `allergies` | **Import Allergies** | opens a "Select Allergies" picker: rows (checkbox, Drug Name, Date Noted, Reaction, Status) from `searchPatientAllergies`; All / None links; "Show Inactive" toggle (default off); Done appends per selected row, legacy format: `Allergen: {drugName} \| Reaction: {reaction}` + newline. |
| `medications` | **Import Medications** | same picker shape from `searchPatientMedications` (Drug Name, Prescriber, Start Date, Status); Done appends per selected row, legacy format: `Medication Name: {drugName}`, then `Prescriber: {prescriber} \| Start Date: {MM/dd/yyyy}` when present, then `Instructions: {instructions}`, blank line between meds. |

Text insertion uses the existing `insertMacro(fieldId, text)` caret-aware helper. The
chart-level Procedures shortcut stays (previously approved).

## 3. Tests

- Backend: handler/validator tests for the two new slices + ensure + categoryId filter
  (existing InMemory harnesses).
- Playwright: one spec covering the Diagnostic Codes dialog (open from Procedures Performed →
  category browse mocked → add → dupe-warning → save PUT payload) and the Plan-field
  affordance placement.

## Out of scope

- Reordering incident dx (legacy drag-and-drop `aidOrder`) — the incident join has no order
  column today.
- Migrating legacy `ascDiagnosticCategories`' 3 custom-code refs.
- Dictation interlocks around the legacy links.
