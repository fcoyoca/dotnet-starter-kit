# Patient Problem List

A patient's diagnosis-based **problem list** (legacy BackChart `PatientProblems`).
Each problem references a diagnostic from the Administration catalog and snapshots
its code + description onto the row (so the list renders without a cross-module
read — matching legacy `pptName`/`ldxDescription`). Problems carry a clinical
status, a diagnosis date, free-text notes, and a medical-alert flag, and can be
associated with patient reports.

## Model

`PatientProblem` (Patient module, `AggregateRoot<Guid>`, soft-deletable):
`PatientId`, optional `IncidentId`, `DiagnosticId` (bare id → Administration
diagnostics), `DiagnosticCode` + `DiagnosticDescription` (snapshot),
`DiagnosisDate?`, `Status`, `Notes?`, `IsMedicalAlert`, audit fields.

**Status** (`ProblemStatus`): `Active`, `Resolved`, `Inactive` (legacy 1/2/3).
The list shows **Active** by default; Resolved/Inactive are opt-in filters.

## API (under `/api/v1/patient`)

| Method | Route | Purpose |
|---|---|---|
| GET | `/problems?patientId=&includeResolved=&includeInactive=&medicalAlertsOnly=` | Search a patient's problems |
| GET | `/problems/{id}` | Get one |
| POST | `/problems` | Add a problem |
| PUT | `/problems/{id}` | Update a problem |
| DELETE | `/problems/{id}` | Soft delete |
| PUT | `/reports/{id}/problems` | Set the problems associated with a report |

Permissions: `Patient.Problems.{View,Create,Update,Delete}`. Associating problems
with a report uses `Patient.Reports.Update`.

## Report association

A report can reference any of the patient's problems (`PatientReportProblem` join,
bare `ProblemId`). The report detail returns `AssociatedProblemIds`; the report
editor's **Associated Problems** panel toggles them and saves via
`SetReportProblems`. Allowed after signing (curation, not part of the locked body).

## Medical alerts

Problems flagged `IsMedicalAlert` surface in a **Medical Alerts** banner at the top
of the patient chart (alongside the demographic medical-alert note).

## Frontend (dashboard)

- `src/api/problems.ts` — typed client (`searchPatientProblems`, `createProblem`,
  `updateProblem`, `deleteProblem`, `setReportProblems`).
- `src/pages/patient-charts/chart.tsx` — a **Problem List** button (chart-actions
  cluster, mirroring BackChart's chart card) + the medical-alert banner.
- `src/pages/patient-charts/problem-list-dialog.tsx` — the list dialog the button
  opens (status filters, add / edit / delete).
- `src/pages/patient-charts/problem-dialog.tsx` — add/edit dialog with DX-code search.
- `src/pages/patient-charts/report-editor.tsx` — Associated Problems panel.

## Changelog

- **Added** — Patient Problem List: diagnosis-based problems per patient with
  status workflow, medical-alert surfacing, and report association.

> Mirror this entry into the separate docs site
> (`github.com/fullstackhero/docs` → `src/content/docs/changelog/`) per golden
> rule #10 — that repo is not cloned in this workspace.
