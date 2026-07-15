# SuperBill Insurance-Type Snapshot Implementation Plan

> **Status: IMPLEMENTED 2026-07-15** — Tasks 1–8 and 10 done (backend `ef589337`, frontend `a0f021dd`); backend + Playwright tests green. Task 9 (freeze-on-signed) intentionally deferred until a billing-provider module lands.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist the insurance type a super bill was priced/billed under, captured at save time, so a report's Procedures Performed history stays correct after the patient's insurance changes. Today insurance type is derived live from the patient's *current* primary policy — so changing that policy retroactively alters what every past report's picker defaults to and what the incident-info sidebar displays. After this change: a new super bill still defaults from the patient's current primary (unchanged UX), but on save it stores the selected `InsuranceTypeId` on the `SuperBill`; reopening an existing super bill shows the *stored* type, not the patient's current one. This mirrors the existing charge-snapshot pattern (each `SuperBillProcedure` already snapshots `Code`/`Description`/`Charge`).

**Non-goal / explicitly out of scope:** a per-incident insurance-type field (incidents deliberately carry none — a super bill is report-level, which matches billing granularity). Freezing the value once a report is signed or `IsBilled` is deferred until a billing-provider module lands (see Task 9).

**Architecture:** `SuperBill` (aggregate root, `Domain/SuperBill.cs`) gains a nullable `Guid? InsuranceTypeId`. It is a **snapshot, not an FK** — insurance types live in the Administration module's schema, and the codebase already avoids cross-module FKs for exactly this reason (`SuperBillProcedure.Code`/`Description` follow the same rule). The value flows in through `SetReportProceduresCommand.InsuranceTypeId`, is set by the handler on create-or-replace alongside `ReplaceProcedures`, and is echoed on `SuperBillSavedIntegrationEvent` so billing-provider subscribers know the payer. Reads project it into `SuperBillDto.InsuranceTypeId`; the dashboard maps id→name via the `useInsuranceTypeOptions` list it already loads, so **no cross-module read** is added to the query handler. The dialog's default precedence becomes: **stored super-bill type (existing bill) → patient's primary active policy type (new bill) → unset**.

**Tech Stack:** Backend — .NET, Mediator (`ICommand`/`ICommandHandler`), EF Core (PostgreSQL, one migration project), `IEventBus`. Frontend — React 19 + TanStack Query v5 + TypeScript, Playwright route-mocked E2E. Migration via `dotnet ef migrations add` against `PatientDbContext`.

## Global Constraints

- **No stored procedures** — hard project rule; all behavior stays in C# Mediator handlers, DB gets a column only.
- **Snapshot, not FK** — `InsuranceTypeId` is a plain nullable `Guid` column; no relationship to Administration's `InsuranceType`. Never resolve the name in the Patient query handler (no cross-module coupling); the dashboard resolves it from its cached options.
- **Nullable + additive** — the column is nullable so existing rows migrate cleanly; the command/DTO/event field is optional so nothing breaks if omitted.
- **Keep `SuperBillSavedIntegrationEvent` name + namespace stable** — adding a trailing field is a compatible change for consumers; do not rename or move the type.
- **Playwright route-mocked E2E only** — no vitest/jest in the dashboard; backend gets xUnit handler tests under `src/Tests/Patient.Tests`.

## Tasks

### Task 1 — Domain: `SuperBill.InsuranceTypeId`
- [ ] `Domain/SuperBill.cs`: add `public Guid? InsuranceTypeId { get; private set; }`.
- [ ] Add param to `Create(Guid reportId, Guid patientId, Guid? insuranceTypeId)` and set it.
- [ ] Add `public void SetInsuranceType(Guid? insuranceTypeId)` that assigns the field and bumps `UpdatedAtUtc` (or fold the assignment into `ReplaceProcedures` — pick one; a dedicated setter keeps `ReplaceProcedures` focused).

### Task 2 — EF config + migration
- [ ] `Data/Configurations/SuperBillConfiguration.cs`: `builder.Property(x => x.InsuranceTypeId);` (nullable, no index needed yet — add `HasIndex` only if a billing query needs it later).
- [ ] Build the solution first (snapshot must be current), then:
      `dotnet ef migrations add AddSuperBillInsuranceType --project src/Host/FSH.Starter.Migrations.PostgreSQL --startup-project src/Host/FSH.Starter.Api --context PatientDbContext`
- [ ] Confirm the generated migration adds a single nullable `uuid` column to `SuperBills` and updates `PatientDbContextModelSnapshot`.

### Task 3 — Contracts
- [ ] `Contracts/v1/SuperBills/SetReportProceduresCommand.cs`: add trailing `Guid? InsuranceTypeId`.
- [ ] `Contracts/Dtos/SuperBillDto.cs`: add `Guid? InsuranceTypeId` (id only — no name; the dashboard maps it).
- [ ] `Contracts/Events/SuperBillSavedIntegrationEvent.cs`: add trailing `Guid? InsuranceTypeId`.

### Task 4 — Command handler + validator
- [ ] `SetReportProceduresCommandHandler.cs`: pass `command.InsuranceTypeId` into `SuperBill.Create` (new bill) and call `bill.SetInsuranceType(command.InsuranceTypeId)` before/with `ReplaceProcedures`; include `InsuranceTypeId` in the published `SuperBillSavedIntegrationEvent`.
- [ ] `SetReportProceduresCommandValidator.cs`: `InsuranceTypeId` optional; if present, `NotEqual(Guid.Empty)`. No existence check (would require a cross-module query — consistent with the snapshot rule).

### Task 5 — Read model
- [ ] `GetReportProcedures/GetReportProceduresQueryHandler.cs`: project `bill.InsuranceTypeId` into `SuperBillDto`; the empty-shell (no bill yet) path returns `InsuranceTypeId = null`.

### Task 6 — Backend tests
- [ ] `src/Tests/Patient.Tests/Features/...` (mirror existing SuperBill handler test): saving with an `InsuranceTypeId` persists it and it round-trips through GET; the published event carries it; saving with `null` leaves it null. Validator rejects `Guid.Empty`.

### Task 7 — Frontend api types
- [ ] `clients/dashboard/src/api/report-procedures.ts`: add `insuranceTypeId?: string | null` to `SuperBillDto`, to `SetReportProceduresInput`, and include it in the PUT body in `setReportProcedures`.

### Task 8 — Dialog: stored type wins over patient primary
- [ ] `procedures-performed-dialog.tsx`: when hydrating an existing bill (`proceduresQuery.data`), if `insuranceTypeId` is present set the picker to it (once, alongside the existing `hydratedFor` guard). Only run the patient-primary default when the loaded bill has **no** stored type (new/empty bill). Precedence: stored → patient primary (known option) → unset.
- [ ] On `onSave`, include `insuranceTypeId: insuranceTypeId ?? null` in `setReportProcedures` payload.

### Task 9 — (Deferred, note only) freeze-on-signed
- [ ] Decide, when billing lands, whether a signed report / `IsBilled` bill locks `InsuranceTypeId`. Until then it reflects the most recent save. Leave a code comment on `SetInsuranceType` pointing here.

### Task 10 — Frontend E2E
- [ ] Extend `tests/patient-charts/procedures-performed.spec.ts`: mock `getReportProcedures` returning a bill whose `insuranceTypeId` differs from the patient's primary → assert the picker shows the **stored** type, not the patient's; assert the PUT body includes `insuranceTypeId`.

## Verification
- [ ] `dotnet build` + backend tests green; `DbMigrator apply` runs the new migration on a dev stack.
- [ ] `tsc -b` + `eslint` clean; the procedures-performed spec (all cases) green.
- [ ] Manual: change a patient's primary insurance after saving a bill → the saved report's picker still shows the originally-billed type; a brand-new report defaults from the new primary.
