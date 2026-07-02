# Allergy List, Medication List & Patient Notes — Design

**Date:** 2026-07-02 · **Branch:** `clinic-app` · **Status:** Approved

Port BackChart's Allergy List, Medication List, and Patient Notes features into
clinic-solution-app, integrated into the patient chart as shortcut-button dialogs the same
way BackChart-FE presents them on the patient chart card (`PatientChartCard.razor`) and the
same way the already-ported Problem List works here.

Legacy references:

- `BackChart-FE/BackChart/Pages/PatientCharts/{Allergies,Medications,Notes}/*.razor`
- `backchart-master/DotNet/services/Patients.aspx` (`PatientAllergies_*`, `PatientMedications_*`,
  `PatientNotes_*`), `Snomed.aspx` (`SnomedAssociation_Get`), `Demographics.aspx` (`RXNCONSO_Get`),
  `MedicationReconciledDates.aspx`

New-app patterns to mirror:

- Backend: `Modules.Patient` PatientProblems slices (`Features/v1/PatientProblems/*`,
  `Domain/PatientProblem.cs`)
- Reference data: `Modules.Administration` Diagnostics catalog (entity + paginated search +
  CRUD + `clients/dashboard/src/pages/administration/diagnostics.tsx`)
- Frontend: `clients/dashboard/src/pages/patient-charts/problem-list-dialog.tsx` opened from
  the chart-actions row in `chart.tsx`
- Migration: DbMigrator verbs `migrate-lookups-from-mssql` / `migrate-from-mssql`

## Decisions made during brainstorming

1. **Drug names**: migrate the legacy RxNorm `RXNCONSO` table into a local `Drug` catalog in
   the Administration module, with full admin CRUD, plus an **admin-controlled on-demand
   RxNav sync** (admin searches the NIH RxNav REST API by term and imports selected matches).
   No automatic or bulk sync now; monthly-file bulk import is a possible follow-up.
2. **Allergy reactions**: curated `AllergyReaction` lookup (from legacy
   `SnomedAssociations` ⋈ `Snomed`, reaction rows only) with admin CRUD. The allergy row keeps
   a single free-text `Reaction` field (legacy `paReaction varchar(1000)`); the picker appends
   selected terms into it.
3. **Medication reconciliation**: included — but legacy's "Browse for CCD" is a stub that
   toasts "not available", so the port is: reconciliation dialog (current meds view) +
   "Mark Reconciled Today" action + Dates Reconciled history. No CCD import.
4. **Patient-level flags**: port both `pNoAllergies` → `NoKnownAllergies` and
   `pNoMedications` → `NoKnownMedications`, with legacy rules.

## 1. Chart integration

Three new buttons in the chart-actions row of
`clients/dashboard/src/pages/patient-charts/chart.tsx` (next to Problem List, whose comment
already anticipates them): **Allergy List**, **Medication List**, **Patient Notes**. Each is
gated on its `view` permission and opens the corresponding dialog. Notes flagged
`IsMedicalAlert` surface in the chart's existing medical-alert affordance alongside alert
problems.

## 2. Backend — Patient module

New aggregates modeled on `PatientProblem` (private setters, `Create`/`Update` factories,
audit-name snapshots, `Guid.CreateVersion7()` ids), each with Search / GetById / Create /
Update / (Delete) slices, validators, endpoints wired in `MapEndpoints()`, and one EF
migration in `FSH.Starter.Migrations.PostgreSQL/Patient/`.

### PatientAllergy (legacy `PatientAllergies`)

| Field | Notes |
| --- | --- |
| `PatientId` | FK |
| `DrugName` (required) | from drug picker or free text (legacy `paDrugName`) |
| `RxAui?` | legacy `paRXAUI` |
| `Reaction?` | text ≤1000, built by reaction picker + free text (`paReaction`) |
| `Comments?` | `paComments` |
| `DateNoted` (required) | `paDateNoted` |
| `IsActive` | active/inactive instead of delete (legacy semantics) |
| audit | CreatedBy/Name/AtUtc, UpdatedBy/Name/AtUtc |

No soft delete — deactivation is the legacy behavior; no hard-delete endpoint.

### PatientMedication (legacy `PatientMedications`)

`DrugName` (required), `RxAui?`, `RxCode?` (RXCUI; drives MedlinePlus Info link
`https://connect.medlineplus.gov/application?...&mainSearchCriteria.v.c={RxCode}`), `Ndc?`,
`Prescriber?`, `StartDate` (required), `EndDate?`, `DoseValue?` (decimal),
`DoseUnitId?` (int, Administration `MedicationDoseUnit` bare cross-schema id),
`DosePeriodValue?` (decimal), `DosePeriodUnit?` (string — static period list like legacy),
`Instructions?`, `Indication?`, `IsActive`, audit. Same active/inactive semantics.

### PatientNote (legacy `PatientNotes`)

`Name` (required), `Description?`, `IsMedicalAlert`, `ISoftDeletable` (legacy `pnDeleted`),
audit. Slices include Delete (soft).

### MedicationReconciledDate (legacy `MedicationReconciledDates`)

`PatientId`, `ReconciledOn` (date), `CreatedByUserId/Name`, `CreatedAtUtc`.
Commands/queries: `MarkMedicationsReconciled` (creates a row for today),
`GetMedicationReconciledDates` (history list).

### Patient flags

The `Patient` aggregate root gains `NoKnownAllergies` and `NoKnownMedications` (bool
columns on the Patients table, default false), exposed through
Get/Create/Update patient DTOs plus two focused commands
`SetPatientNoKnownAllergies` / `SetPatientNoKnownMedications` used by the dialogs' checkboxes.
Server-enforced rules (mirroring legacy client rules):

- Setting a flag **true** while active allergies/medications exist → 409 Conflict with the
  legacy message ("You cannot set No Allergies when Active allergies exist." / medications
  equivalent).
- Creating or updating an allergy/medication to **active** auto-clears the corresponding flag
  in the same handler/transaction.

### Permissions

`Permissions.Patient.Allergies.{View,Create,Update,Delete}`,
`Permissions.Patient.Medications.{View,Create,Update,Delete}`,
`Permissions.Patient.Notes.{View,Create,Update,Delete}` — registered in the module permission
registry and seeded to the same roles the Problems permissions use. (Delete on
allergies/medications guards the Active/Inactive toggle, matching legacy
`ALLERGYLISTDELETE`/`MEDICATIONLISTDELETE`; Notes Delete guards the soft delete.
Reconciliation actions use Medications permissions.)

## 3. Reference data — Administration module

Three new entities + slices + admin CRUD, following the Diagnostics pattern. All are
`IGlobalEntity` lookups like the existing Administration lookups, preserving legacy integer
ids where migrated.

- **`Drug`** (legacy `RXNCONSO`): `Id`, `RxAui` (unique), `RxCui`, `Name` (indexed for
  search), `Tty?`, `IsActive`. Paginated search endpoint (name prefix/contains) consumed by
  the chart drug pickers; full CRUD for admins.
- **`AllergyReaction`**: `Id`, `SnomedCode`, `Term`, `IsActive`; list endpoint + CRUD.
- **`MedicationDoseUnit`** (legacy medication unit types): `Id`, `Name`, `IsActive`; list
  endpoint + CRUD.

### RxNav on-demand sync (admin-only)

- `GET /api/v1/administration/drugs/rxnav?term=...` — backend proxies the NIH RxNav REST API
  (`https://rxnav.nlm.nih.gov/REST/drugs.json?name={term}`) through a named HttpClient with
  standard Polly resilience (per `.agents/rules/resilience.md`), mapping concepts to
  candidate rows (RxCui, name, TTY; synthesized/absent RxAui allowed — RxNav is
  RXCUI-centric).
- `POST /api/v1/administration/drugs/import` — imports the admin-selected candidates
  (upsert by RxCui/RxAui).
- Both gated by the Drugs create/update permissions; no background or scheduled sync.

## 4. Frontend — dashboard

New API modules `src/api/allergies.ts`, `src/api/medications.ts`, `src/api/patient-notes.ts`;
drugs / allergy-reactions / dose-units functions added to `src/api/administration.ts`.
Per-call data passes through `mutate(arg)` (golden rule 9); query keys inline like
`["patient-allergies", patientId, { showInactive }]`.

### AllergyListDialog (`allergy-list-dialog.tsx`)

Table: drug name, reaction, comments, date noted, status badge. Controls: **Show Inactive**
toggle (client filter of the active-included query, like legacy), **Set No Allergies**
checkbox (fires the focused command; surfaces the 409 message as a toast), **Add Allergy**.
Add/edit sub-dialog: drug-search combobox (Administration drug search, free-text allowed),
reaction multi-pick from `AllergyReaction` list appending terms into the editable Reaction
text field, Comments, Date Noted (required), Active/Inactive radio (gated on
`Allergies.Delete`), created/modified footer.

### MedicationListDialog (`medication-list-dialog.tsx`)

Table: drug name, dose summary, start/end dates, status. Controls: Show Inactive, **Set No
Medications**, **Add Medication**, **Reconciliation**. Add/edit sub-dialog: drug-search
combobox, Prescriber, Start Date (required), End Date, Dose Value + Dose Unit (lookup) +
Period Value + Period Unit (static list), Special Instructions, Indications, Active/Inactive
(gated on `Medications.Delete`), **Info** button (MedlinePlus link, shown when RxCode
present). Reconciliation sub-dialog: current medications table + **Mark Reconciled Today** +
Dates Reconciled history list.

### PatientNotesDialog (`patient-notes-dialog.tsx`)

Table: name, description, created date, Is Alert. Add/edit form: Name (required),
Description, Medical Alert checkbox; Delete (soft) with confirm. Closing the dialog refreshes
the chart's medical-alert data.

### Admin pages

`pages/administration/drugs.tsx` (paginated search + CRUD + "Search RxNav" import flow),
`allergy-reactions.tsx`, `medication-dose-units.tsx` — all following the existing
administration page/table/dialog conventions, registered as lazy routes with mirrored
permissions.

### Permissions mirror

`patient-permissions.ts` gains `ALLERGY_PERMISSIONS`, `MEDICATION_PERMISSIONS`,
`NOTE_PERMISSIONS`; administration permission mirrors gain Drugs / AllergyReactions /
MedicationDoseUnits.

## 5. Data migration — DbMigrator

- New verb **`migrate-drug-catalog-from-mssql`**: copies `RXNCONSO` → `Drugs` in batches
  (large table — stream with a plain reader, batched inserts), `SnomedAssociations` (reaction
  rows, joined to `Snomed` for code/term) → `AllergyReactions`, legacy medication unit types →
  `MedicationDoseUnits`. Preserves legacy integer ids and resets identity sequences, matching
  `MssqlLookupMigrationRunner` semantics. Run before patient-data migration.
- Extend **`migrate-from-mssql`**: `PatientAllergies`, `PatientMedications`, `PatientNotes`,
  `MedicationReconciledDates` rows mapped to the new tables (patient resolved via
  `Patient.LegacyUniqueId`), plus `pNoAllergies`/`pNoMedications` onto the patient flags.
  Legacy column plaintext/encryption status to re-verify against the live DB during
  implementation (these tables are expected plaintext — not in the known encrypted-column
  list).

## 6. Error handling & testing

- Every command handler and paginated query handler gets a `{Name}Validator`
  (architecture-test enforced). Handlers `public sealed`, `ValueTask<T>`,
  `ConfigureAwait(false)`, CancellationToken propagation, structured logging.
- Conflict rules (No-Allergies/No-Medications) return 409 with legacy-equivalent messages;
  missing rows → NotFound.
- Unit tests per handler in `src/Tests` mirroring the PatientProblems test suites (including
  flag auto-clear and 409 paths, RxNav mapper tests with a stubbed handler).
- Playwright tests remain deferred, consistent with prior Patient-module sprints.
- Docs/changelog: follow repo convention for this fork (golden rule 10 applies to the
  upstream starter's docs repo; this private fork tracks progress in specs + memory).

## Out of scope

- CCD/CCDA document import (stubbed in legacy too)
- Health-history variants of the allergy/medication views (`HealthHistoryView` mode)
- Bulk RxNorm monthly-file import job (possible follow-up)
- Medication import from patient-chart reports (`IsSelected` flow)
