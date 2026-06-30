# Patient Reports

Clinical reports authored against a patient **incident**, built on tenant-defined
report templates (Administration `ReportType` + `ReportField`, grouped by category,
plus `Macro`s). A clinician fills templated fields and vitals, signs the report
(electronic attestation **plus** a snapshot of the provider's saved signature
image), can request a peer review, append addendums after signing, and search a
report's text.

## Workflow

`Draft → Signed → ReviewRequested → Reviewed`

- **Draft** — editable header (report type, date, provider, clinic, no-show),
  vitals (height/weight/BMI auto-computed, BP, pulse, temperature), and field
  sections grouped by `ReportField.category`. Per-field **macro insert** splices
  template text at the caret. Save Draft is an upsert; field values are stored
  sparsely (only filled fields).
- **Signed** — the report locks (edits rejected server-side; addendums are the
  post-sign channel). The signer is taken from the current user; the signature
  image is snapshotted from the report's provider (optional — null when the
  provider has none).
- **Review** — a reviewer is chosen and `Request Review` moves the report to
  `ReviewRequested`; `Review-Sign` snapshots the reviewer provider's signature and
  moves it to `Reviewed`.

## API (under `/api/v1/patient`)

| Method | Route | Purpose |
|---|---|---|
| GET | `/reports?incidentId=&patientId=&search=` | Search (optional phrase across field values + addendums) |
| GET | `/reports/{id}` | Detail (header + vitals + field values + addendums) |
| POST | `/reports` | Create (header only) |
| PUT | `/reports/{id}` | Update draft (header + vitals + field values) |
| PUT | `/reports/{id}/sign` | Sign (attestation + provider signature snapshot) |
| POST | `/reports/{id}/addendums` | Append an addendum |
| PUT | `/reports/{id}/request-review` | Request peer review |
| PUT | `/reports/{id}/review-sign` | Review-sign |
| DELETE | `/reports/{id}` | Soft delete |

Permissions: `Patient.Reports.{View,Create,Update,Sign,Review,Delete}`.

`ReportTypeId`/`ReportFieldId` are stored as **bare ints** (no FK to the
Administration schema), mirroring `PatientIncidentDiagnostic`; the editor resolves
names via the admin list APIs.

## Import Dx Codes

Fields named **Clinical Impression** or **Assessment** (legacy `ldfID` 15 / 22)
show an **Import Dx Codes** action beside the field's Macro button. It appends the
report's associated problems as `{code} - {description}` lines into that field
(mirrors BackChart's inline "Import Dx Codes"). Associate problems first via the
Associated Problems panel.

## Frontend (dashboard)

- `src/api/reports.ts` — typed client mirroring the Contracts DTOs.
- `src/components/ui/macro-insert.tsx` — per-field macro popover.
- `src/pages/patient-charts/report-editor.tsx` — editor + read-only signed view +
  addendums + peer-review actions (route
  `/patient-charts/:patientId/reports/:reportId`).
- `src/pages/patient-charts/chart.tsx` — Patient Reports panel + Add Report menu
  for the active incident.
- `src/pages/patient-charts/report-search-dialog.tsx` — phrase search over a
  incident's reports.

## Changelog

- **Added** — Patient Reports: template-driven clinical reports per incident, with
  vitals, provider-signature attestation, peer review, addendums, and report text
  search.

> Mirror this entry into the separate docs site
> (`github.com/fullstackhero/docs` → `src/content/docs/changelog/`) per golden
> rule #10 — that repo is not cloned in this workspace.
