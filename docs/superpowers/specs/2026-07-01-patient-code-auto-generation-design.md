# Auto-Generated Sequential Patient Codes — Design

**Date:** 2026-07-01
**Status:** Approved (design)
**Area:** Patient module (`src/Modules/Patient`) + dashboard (`clients/dashboard`)

## Problem

Today the "Register a patient" dialog requires the user to type a `PatientCode`
by hand. This invites typos, inconsistent formats across staff, and possible
duplicate-key conflicts. We want the code generated automatically, in a format
that:

1. Can never collide, even under concurrent creation across clinics (this app
   is multi-tenant via a **shared database** with a `TenantId` discriminator
   column — `PatientCode`'s unique index has no tenant column, so uniqueness is
   already global across every clinic today).
2. Looks and feels like the same "family" of code as patients migrated from
   legacy BackChart, so old and new patients aren't visually jarring side by
   side.

## Key finding that scopes the feature

The MSSQL migration importer (`MssqlPatientMapper.cs`) already supplies its own
explicit code — `PatientCode: $"P-{pId}"` — directly to `CreatePatientCommand`.
**This feature only touches the manual "Register a patient" dialog.** The
migration path is untouched: nothing to reconcile, no shared numbering to
coordinate between old and new patients.

## Decisions (from brainstorming)

1. **Generation mechanism: a real Postgres SEQUENCE**, not a computed
   `MAX(existing)+1` with retry. A DB sequence hands out `nextval()` atomically
   — two concurrent callers can never receive the same number, so no retry
   loop is needed anywhere in application code.
2. **Format: `P-<number>`** — same dash-separated shape as migrated codes
   (`P-{legacyPId}`) and the existing dialog's placeholder text. The sequence
   starts at **100,000** so its output can never numerically collide with a
   small legacy `pID` (which is at most a few digits for any single clinic).
   Zero-padding is unnecessary — the floor already guarantees 6+ digits, and
   the count only grows.
3. **One global sequence**, not one per tenant. Since `PatientCode` uniqueness
   is already enforced globally (single-column unique index, no tenant scoping),
   a single sequence is both sufficient and required — a per-tenant sequence
   would let two different clinics generate the same numeric suffix, which
   the existing unique index would then reject as a false conflict.
4. **Claim timing: preview only, assign at Save.** The dialog shows a
   best-effort "next code" preview when it opens; the authoritative code is
   assigned atomically inside the `CreatePatient` transaction. Consequence:
   cancelling the dialog wastes nothing (no number is ever consumed for a
   cancelled draft), but if two staff members have the dialog open
   simultaneously, the previewed number may occasionally not match what is
   actually saved. This is a cosmetic edge case, not a correctness issue — the
   saved code is always correct and always unique.
5. **`CreatePatientCommand.PatientCode` becomes optional.** When null/blank,
   the handler auto-generates via the sequence. When provided — the migration
   importer's path — it's used as-is, exactly as today. This is what keeps the
   migration path untouched.

## Backend changes (Patient module)

- **New sequence**, declared in `PatientDbContext.OnModelCreating`:
  ```csharp
  modelBuilder.HasSequence<long>("PatientCodeSequence", schema: Schema)
      .StartsAt(100_000)
      .IncrementsBy(1);
  ```
  One EF migration adds it (`CreateSequence`) — no data migration, no existing
  rows touched.

- **`CreatePatientCommand`**: `string PatientCode` → `string? PatientCode = null`
  (trailing optional parameter — the migration mapper uses named arguments, so
  it is unaffected by the reorder-safety of a trailing optional).

- **`CreatePatientCommandValidator`**: replace `RuleFor(x => x.PatientCode).NotEmpty().MaximumLength(50);`
  with a conditional max-length rule only (null is now valid input):
  ```csharp
  RuleFor(x => x.PatientCode).MaximumLength(50).When(x => x.PatientCode is not null);
  ```

- **`CreatePatientCommandHandler`**: before the existing `codeTaken` check,
  resolve the code:
  ```csharp
  string patientCode = string.IsNullOrWhiteSpace(command.PatientCode)
      ? await GenerateNextCodeAsync(dbContext, cancellationToken).ConfigureAwait(false)
      : command.PatientCode;
  ```
  `GenerateNextCodeAsync` executes `SELECT nextval('"patient"."PatientCodeSequence"')`
  (schema-qualified, matching wherever EF creates it) and formats `$"P-{n}"`.
  The existing duplicate-key check (`codeTaken`) stays as a defense-in-depth
  backstop regardless of source — it should never fire for a sequence-generated
  code, but costs nothing to keep.

- **New preview endpoint**: `GET /api/v1/patient/patients/next-code-preview`
  → `{ "preview": "P-100007" }`. Implementation peeks the sequence without
  consuming it, using the standard Postgres idiom of selecting directly from
  the sequence object:
  ```sql
  SELECT last_value, is_called FROM "patient"."PatientCodeSequence";
  ```
  Preview = `is_called ? last_value + 1 : last_value`. This is explicitly a
  **best-effort preview**, not a reservation — see Decision 4. Gated by
  `PatientPermissions.Patients.Create` (same permission as `CreatePatient` —
  the preview is only ever useful immediately before a create).

## Frontend changes (dashboard)

- `src/api/patients.ts`: add a `getNextPatientCodePreview(): Promise<string>`
  call against the new endpoint; `CreatePatientInput`/`PatientFields.patientCode`
  becomes optional in the type (still present for the migration/import paths
  that may exist elsewhere, but the dashboard's create flow stops sending it).
- The shared `create-patient-dialog.tsx` (from the Patient Chart search
  feature): the "Patient code" field becomes **read-only**, pre-filled from
  the preview call when the dialog opens (`open` transitions to `true`). A
  short helper line clarifies "Assigned automatically."
- On submit, the create payload omits `patientCode` entirely (or sends
  `undefined`) — the server always assigns the authoritative code.

## Testing

- Backend:
  - `CreatePatientCommandValidator` test: `PatientCode: null` is now valid.
  - `CreatePatientCommandHandler` test: null/blank `PatientCode` triggers
    generation (format `P-<number>`); a provided `PatientCode` is used as-is
    (covers the migration-import path continuing to work).
  - A focused test asserting two consecutive `nextval()` calls never repeat
    (sequence behavior sanity check, not a concurrency stress test).
- Frontend (Playwright): the create dialog's Patient code field is disabled
  and pre-filled from the mocked preview endpoint; the submitted POST body has
  no `patientCode` key.

## Edge cases

- **Preview endpoint unavailable / slow**: the dialog degrades to a disabled
  field showing a loading state; Save remains usable regardless (the server
  generates the code either way — the preview is cosmetic).
- **Concurrent creates**: covered by Decision 1 — the sequence itself
  guarantees no duplicate is ever issued, independent of the preview.
- **Migration importer**: entirely unaffected — continues to pass its own
  `P-{legacyPId}` code, never touches the sequence.

## Non-goals

- No change to the MSSQL migration importer or its code format.
- No per-tenant sequence.
- No editable manual-override path in the "Register a patient" dialog (if a
  clinic later needs to assign a custom code by hand, that's a separate,
  future ask).
- No change to `CreatePatient`'s response shape — it still returns only the
  new patient's id; the assigned code is visible once the frontend navigates
  to the chart.

## Affected files (summary)

Backend:
- `src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs` (new sequence)
- New EF migration (adds the sequence)
- `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/CreatePatientCommand.cs`
- `src/Modules/Patient/Modules.Patient/Features/v1/Patients/CreatePatient/CreatePatientCommandValidator.cs`
- `src/Modules/Patient/Modules.Patient/Features/v1/Patients/CreatePatient/CreatePatientCommandHandler.cs`
- New endpoint: `GET /api/v1/patient/patients/next-code-preview` (new query/handler/endpoint slice, mirroring the `SearchPatients` slice's shape)

Frontend:
- `clients/dashboard/src/api/patients.ts` (new preview call; optional `patientCode`)
- `clients/dashboard/src/pages/patients/create-patient-dialog.tsx` (read-only, pre-filled code field)

Tests:
- Patient module: validator + handler unit tests for optional/auto-generated code.
- Dashboard: Playwright coverage for the read-only pre-filled field and the omitted `patientCode` in the POST body.
