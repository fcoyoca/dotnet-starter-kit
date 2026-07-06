# Procedures Performed (Super Bill) — Design

**Date:** 2026-07-06 · **Branch:** `clinic-app` · **Status:** Approved

Port BackChart's **Procedures Performed** feature (the per-report super-bill editor) into
clinic-solution-app: record which procedures were administered in a report, which of the
incident's Dx codes justify each procedure, and the charge per procedure — with the
procedure's macro text optionally appended to the report's Plan field.

Legacy references:

- `BackChart-FE/BackChart/Pages/PatientCharts/VisualReports/ProceduresPerformed/
  ProceduresPerformedDialog.razor` + `ProceduresPerformedIndex.razor`
- `backchart-master/DotNet/services/SuperBillProcedures.aspx`
  (`SuperBills_Insert`, `SuperBillProcedures_Set_Multi` = delete-all-then-reinsert,
  `SuperBillProcedures_Get_XML`), tables `SuperBills`
  (`sbID`, `sbReportID`, `sbBilled`, `sbBilledDate`) and `SuperBillProcedures`
  (`sbpSuperBillID`, `sbpProcedureCodeID`, `sbpDiagnosticsID`, `sbpCharge`)

New-app patterns to mirror:

- Backend: `Modules.Patient` PatientReports slices; `PatientIncidentDiagnostic` join-row
  pattern; bare cross-module ids (like `PatientReport.AppointmentId`)
- Reference data (already migrated): `Modules.Administration` `ProcedureCategory`,
  `ProcedureCode` (with `MacroText`), `InsuranceType`, `InsuranceTypeProcedure` (`Price`),
  `CustomDiagnostic` (incident Dx codes)
- Frontend: chart-shortcut dialogs in `clients/dashboard/src/pages/patient-charts/`;
  report editor's `insertMacro` / `DX_IMPORT_FIELDS` name-matching; `mutate(arg)` rule

## Decisions made during brainstorming

1. **No stored procedures.** All legacy stored-proc behavior becomes C# Mediator
   handlers with EF Core LINQ; the database gets tables only (one EF migration).
2. **UI in both places**: a *Procedures Performed* button on the report editor **and** a
   chart-level shortcut that first asks which report (Export Reports pattern), then opens
   the same dialog.
3. **Model the super bill now**: a `SuperBill` aggregate (with `IsBilled` /
   `BilledDateUtc` for the future medical-billing module), not just bare report
   procedures. Billing workflow UI, Kareo/Cvikota exports are **out of scope**.
4. **Dx source = incident diagnostics** (faithful to BackChart): checkboxes come from the
   report's incident `diagnosticIds` (CustomDiagnostic guids); *Edit Dx Codes* opens the
   existing incident edit dialog. Report-level Associated Problems are not involved.

## 1. Backend — Patient module

### Domain

`SuperBill` (`AggregateRoot<Guid>`, `Domain/SuperBill.cs`):

| Property | Type | Legacy | Notes |
|---|---|---|---|
| `ReportId` | `Guid` | `sbReportID` | unique index — one super bill per report |
| `PatientId` | `Guid` | — | denormalized for chart-level queries |
| `IsBilled` | `bool` | `sbBilled` | always `false` for now (no workflow UI yet) |
| `BilledDateUtc` | `DateTime?` | `sbBilledDate` | null for now |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTime` | — | standard audit stamps |
| `Procedures` | `List<SuperBillProcedure>` | — | owned collection |

`SuperBillProcedure` (child entity, id `Guid.CreateVersion7()`):

| Property | Type | Legacy | Notes |
|---|---|---|---|
| `SuperBillId` | `Guid` | `sbpSuperBillID` | FK |
| `ProcedureCodeId` | `Guid` | `sbpProcedureCodeID` | bare id into Administration (no cross-module FK) |
| `Charge` | `decimal` | `sbpCharge` | ≥ 0 |
| `DisplayOrder` | `int` | — | preserves the on-screen order |
| `DiagnosticIds` | join rows | `sbpDiagnosticsID` | `SuperBillProcedureDiagnostic` (`SuperBillProcedureId`, `DiagnosticId`) mirroring `PatientIncidentDiagnostic` |

Aggregate methods: `SuperBill.Create(reportId, patientId)` and
`ReplaceProcedures(IReadOnlyList<(Guid procedureCodeId, decimal charge, IReadOnlyList<Guid> diagnosticIds)>)`
— clears and re-adds the collection (modern equivalent of the legacy
delete-all-then-reinsert), bumping `UpdatedAtUtc`. Duplicate procedure codes are allowed
(legacy behavior). Hard delete of procedure rows on replace; the `SuperBill` row itself
is never deleted (no soft-delete — same stance as `InsuranceTypeProcedure`).

### Slices (`Features/v1/SuperBills/`)

| Slice | Route | Returns / Accepts |
|---|---|---|
| `GetReportProcedures` | `GET /api/v1/patient/reports/{reportId}/procedures` | `SuperBillDto { id?, reportId, isBilled, billedDateUtc, procedures: [{ id, procedureCodeId, charge, displayOrder, diagnosticIds[] }] }` — empty shell (`id: null`, `procedures: []`) when no super bill exists yet |
| `SetReportProcedures` | `PUT /api/v1/patient/reports/{reportId}/procedures` | `SetReportProceduresCommand { reportId, procedures: [{ procedureCodeId, charge, diagnosticIds[] }] }` → creates the `SuperBill` on first save, then replaces the whole set in one transaction |

Contracts in `Modules.Patient.Contracts` (`v1/SuperBills/`, `Dtos/SuperBillDto.cs`).
Handlers `public sealed`, `ValueTask<T>`, `.ConfigureAwait(false)`, `CancellationToken`
propagated. `SetReportProceduresCommandValidator`: report exists check in handler
(NotFound), `Charge >= 0`, `ProcedureCodeId != Guid.Empty`, each procedure has ≥ 1
diagnostic id, no empty diagnostic guids. An empty `procedures` list is valid (clears the
set). Setting procedures is rejected when the report is signed? **No** — legacy allows
editing the super bill after signing (it's billing data, not chart content), so no
signed-report guard.

### Permissions

New `PatientPermissions.SuperBills` (`Resource = "Patient.SuperBills"`): `View`, `Manage`
(single write permission — the operation is one atomic replace). Registered in
`PatientPermissions.All` (`View` is `IsBasic: true` like sibling view permissions).
`GET` requires `View`; `PUT` requires `Manage`.

### Data

`SuperBillConfiguration` in `Data/Configurations/`: table names `SuperBills`,
`SuperBillProcedures`, `SuperBillProcedureDiagnostics`; unique index on `ReportId`;
composite PK on the diagnostic join; cascade delete super bill → procedures → dx rows;
`Charge` as `decimal(10,2)`. One EF migration in
`FSH.Starter.Migrations.PostgreSQL/Patient/`.

## 2. Frontend — dashboard

### API module (`src/api/report-procedures.ts`)

`getReportProcedures(reportId)`, `setReportProcedures({ reportId, procedures })`, plus the
DTO types. Query key `["report-procedures", reportId]`.

### Dialog (`src/pages/patient-charts/procedures-performed-dialog.tsx`)

Props: `patientId`, `reportId`, `incidentId`, `open`, `onClose`,
`onMacroText?: (text: string) => void`.

Layout (mirrors `ProceduresPerformedDialog.razor`):

1. **Header row**: *Edit Dx Codes* button (opens the existing `incident-dialog` in edit
   mode; on save, incident query invalidates and dx checkboxes refresh) · title
   *Procedures Administered in this Report* · *Add Macro Text to Plan Comments* toggle
   (default on; rendered only when `onMacroText` is provided, i.e. report-editor context).
2. **Current procedures list**: one row per procedure — code, description, editable
   charge input, a checkbox per incident Dx code (checked = linked; label = dx code,
   tooltip = description, resolved via `listCustomDiagnostics`), and a remove button
   (double-click on the row also removes, as in legacy).
3. **Picker filters**: Insurance combobox (`useInsuranceTypeOptions`) — default: the
   incident's insurance type when set, else the patient's top-priority insurance, else
   the first active type (legacy default cascade); Procedure Category combobox
   (`useProcedureCategoryOptions`) with an "All" default.
4. **Procedure picker table**: active codes from
   `listProcedureCodes({ procedureCategoryId, isActive: true, pageSize: 500 })` merged
   client-side with `listInsuranceTypeProcedures(insuranceTypeId)` to show the negotiated
   price (— when unassociated). Double-click (or per-row Add button) appends the
   procedure to the top list with charge defaulted to the price (0 when unpriced) and all
   current incident dx checked (legacy default: new procedure gets the full default dx
   set, checked). Duplicates allowed. Blocked with a warning toast when the incident has
   zero Dx codes ("Please add at least one DX code before picking procedures.").
   When the *Add Macro Text* toggle is on and the code has `macroText`, call
   `onMacroText(macroText)`.
5. **Footer**: Save (primary — `PUT` replace-all from local state via `mutate(arg)`,
   success toast, invalidate `["report-procedures", reportId]`, close) · Close.

Dirty state lives entirely in the dialog; nothing persists until Save (legacy behavior).

### Report editor integration (`report-editor.tsx`)

- *Procedures Performed* button (Stethoscope-adjacent icon) in the sticky action bar,
  visible when the user has `SuperBills.View`; the dialog is read-only-ish without
  `Manage` (Save hidden, inputs disabled).
- `onMacroText` appends into the **Plan** field: name-matched like `DX_IMPORT_FIELDS`
  (`"plan"`, `"plan comments"`, `"treatment plan"`) using the existing `insertMacro`;
  when no such field exists in the report type, fall back to a toast informing the text
  couldn't be placed.
- The report's `incidentId` is already on `PatientReportDetailDto` and the dashboard's
  report types — no contract change needed.

### Chart shortcut (`chart.tsx`)

An `IconShortcut` *Procedures* in the chart-actions row (gated on `SuperBills.View`),
opening a small report-picker step (reuse the report list pattern from
`export-reports-dialog.tsx` / `report-search-dialog.tsx`) and then the same dialog
without `onMacroText` (toggle hidden, no plan-comment side effect).

### Permission constants

Add `SUPERBILL_PERMISSIONS` to `src/lib/patient-permissions.ts` and mirror the new
permission strings wherever the dashboard mirrors patient permissions.

## 3. Tests

- **Backend unit tests** (`src/Tests/Patient.Tests`): `SetReportProceduresCommandHandler`
  (creates super bill on first save; replaces set; rejects unknown report),
  `SetReportProceduresCommandValidator` (negative charge, empty dx list, empty guids),
  `GetReportProceduresQueryHandler` (empty shell vs populated). Architecture tests pick
  up handler/validator conventions automatically.
- **Playwright** (dashboard, route-mocked): open dialog from report editor, add a
  procedure (dx checkbox default state), toggle a dx, save → asserts `PUT` payload;
  zero-dx guard toast.

## 4. Docs & changelog

Update the separate docs repo (`github.com/fullstackhero/docs`): patient-chart page gains
a *Procedures Performed* section; add a changelog entry under
`src/content/docs/changelog/` (golden rule 10).

## Out of scope

- Billing workflow (`IsBilled` stays false; no billed-date UI)
- Kareo / Cvikota / Centricity export paths and `cGenerateCvikotaDocuments`
- Kareo-specific code rendering (`CustomPcCode` modifier concatenation)
- Legacy-data migration of `SuperBills` / `SuperBillProcedures` rows (can join the
  existing DbMigrator `migrate-from-mssql` verb in a later task)
