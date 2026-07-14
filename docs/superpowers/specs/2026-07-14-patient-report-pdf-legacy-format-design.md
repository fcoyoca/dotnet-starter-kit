# Patient report PDF — legacy BackChart format + per-clinic page orientation

**Date:** 2026-07-14
**Status:** Approved

## Problem

The patient-chart report export produces a PDF that does not resemble the report BackChart users
know. [`PatientReportPdfRenderer`](../../../src/Modules/Patient/Modules.Patient/Services/PatientReportPdfRenderer.cs)
renders A4 portrait with an invented header (report type, report date, patient name/code/DOB/gender)
and no clinic identity, no incident context, and no signature images. Page orientation is hardcoded.

The canonical format is the legacy renderer `BCFileGeneration.vb` in `backchart-master`
(`generatePDFTemplate`, lines 996–1075, and `generateReportsAsPDF`, lines 697–992). `BackChart-FE`
does not render PDFs itself — `ExportReport.razor` calls `SecureFileDownload.aspx`, which drives
`BCFileGeneration`, passing a page orientation. In legacy the orientation is the client-level
setting `PRINT_ORIENTATION` (`ClientSettings.aspx`; a Print Settings dropdown exists but is
commented out in `BackChart-FE/Shared/Administrations/ClientSettings.razor`).

## Goals

1. The exported report PDF carries the same information, in the same order, as the legacy PDF.
2. Page orientation is **configured by an administrator**, not hardcoded — scoped **per clinic**.

Non-goals: Word export, the legacy `.zip`/SHA-256 secure-download flow (already implemented),
changing the export dialog's selection UX, and the clinic logo (see Deferred).

## Decisions

- **Orientation is per-clinic**, on the `Clinic` entity, edited in the dashboard's Administration →
  Clinics page. (Legacy scoped it per client/tenant; per-clinic is a deliberate refinement — each
  physical location can print how it prefers.)
- **Full legacy content parity, modern styling.** Every element of the legacy layout is reproduced,
  but with QuestPDF's typography, rules and spacing rather than the 2013 pixel geometry.
- **Keep the `DRAFT — UNSIGNED` watermark.** Legacy has no such thing, but an unsigned report is not
  a finalised clinical record and a printout that outlives the draft must not read as the signed note.
- **Header keeps the patient code, drops gender.** Neither is in the legacy header; the code aids
  identification, gender does not belong in a page header.

## Design

### 1. Data — page orientation on the Clinic

- New enum `PrintOrientation { Portrait, Landscape }` in `Modules.Administration.Contracts`.
- `Clinic.PrintOrientation` (default **`Portrait`** — the value `ExportReport.razor:201` passed).
- EF configuration + one migration under `Migrations.PostgreSQL/Administration/`.
- Threaded through `CreateClinicCommand` / `UpdateClinicCommand`, their validators, and `ClinicDto`.
- The dashboard's [`administration/clinics.tsx`](../../../clients/dashboard/src/pages/administration/clinics.tsx)
  form gains a Portrait/Landscape select.

A merged export may span clinics. That is fine and needs no special handling: each report is already
its own QuestPDF `Page`, so orientation is resolved **per report** from its `ClinicId`, falling back
to `Portrait` when the report has no clinic.

### 2. Export handler — load what the legacy header needs

[`ExportPatientReportsPdfQueryHandler`](../../../src/Modules/Patient/Modules.Patient/Features/v1/PatientReports/ExportPatientReportsPdf/ExportPatientReportsPdfQueryHandler.cs)
today loads only the reports and the patient. It gains three cross-module reads, all through
`Administration.Contracts` (module-boundary rule), plus one storage read:

| Needed | Source |
|---|---|
| DOIV, DOL | `PatientIncident.DateOfInitialVisit`, `.DateOfLoss` (same DbContext) |
| DX codes | `PatientIncident.Diagnostics` → **CustomDiagnostic** Guids → `ListCustomDiagnosticsQuery(ids)` |
| Clinic name + orientation | `GetClinicByIdQuery` per distinct `report.ClinicId` |
| Signature images | `IPatientDocumentStorage.ReadAsync(report.SignatureImagePath / .ReviewSignatureImagePath)` |

`ReportPdfPatientInfo` grows DOIV/DOL/DX; `ReportPdfModel` grows clinic name, orientation, the
signature image bytes, and the report's last-modified date. SHA-256 hashing, password protection and
the PHI export audit are untouched.

### 3. Renderer — legacy structure, modern styling

`PatientReportPdfRenderer.Render` keeps its `IPatientReportPdfRenderer` seam and its
one-`Page`-per-report shape. Per report:

- **Page:** `PageSizes.Letter`, portrait or landscape per that report's clinic (was: A4, always portrait).
- **Header** (repeats on every page of the report, as the legacy template does):
  clinic/company name and report title, centered; patient block — `Patient` · `DOB` · patient code,
  and `DOIV` · `DOL` · `DX`; a horizontal rule beneath. Logo slot left as a marked hook.
- **Body** (mirrors `BCFileGeneration.vb:743–871`):
  report date, `(Modified: …)` when a modified date exists, and the department/provider line; then
  field values grouped by **category** heading → **field name** → text, in template display order.
  Under the `Clinical Exam` category, vitals render **inline** (Height, Weight, BMI, BP, Heart Rate,
  Temperature) rather than in today's bordered box.
- **Addendums:** `Addendum (Name — datetime)` followed by the text.
- **Signatures:** the signature image, then
  `(This report was digitally signed by {name} on {date})` — for both the signer and the reviewer.
- **Footer:** `N of M` page numbers, centered (legacy `%%SP%% of %%ST%%`).
- **Watermark:** `DRAFT — UNSIGNED` retained for unsigned reports.

## Testing

- Unit: renderer produces a non-empty PDF for portrait and for landscape; a report whose clinic is
  landscape yields a landscape page while a portrait-clinic report in the same export does not
  (assert on QuestPDF page size / rendered dimensions).
- Unit: handler resolves DX codes from custom diagnostics, tolerates a report with no `ClinicId`
  (falls back to Portrait), and tolerates a signature path whose file is missing (`ReadAsync` → null)
  by falling back to the text-only signature line.
- Existing export tests (SHA-256, password protection, PHI audit, same-patient guard) must still pass.
- Clinic create/update validator tests cover the new orientation field.

## Deferred

- **Clinic logo.** The legacy header draws a clinic logo top-left; clinic-app has no logo storage
  anywhere. The header renders the clinic name in that position and leaves the logo slot as a
  clearly-marked hook. Adding `Clinic.LogoPath` + an upload flow is a separate, small follow-up.
- **Word export** (`generateReportsAsWord`) — out of scope.
