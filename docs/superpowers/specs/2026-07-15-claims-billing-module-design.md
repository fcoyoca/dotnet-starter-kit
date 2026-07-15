# Claims (Medical-Claims Billing) Module — Design Spec

> **Status:** Approved 2026-07-15 (design). Ready for implementation planning.
>
> **Scope of this slice:** A new `Claims` bounded context that turns saved super bills into
> trackable insurance claims, with a distinct **Billing** menu in the dashboard (worklist +
> claim detail) and a stubbed submission seam. External clearinghouse transport is deliberately
> a later slice (see §9).

## Goal

Give billers a place to work insurance claims that originate from a patient's **super bill**
(procedures performed on a report). Today the super bill publishes
`SuperBillSavedIntegrationEvent` — explicitly documented as "the pluggable billing seam" — but
nothing consumes it. This slice adds the consumer: a `Claim` aggregate, a lifecycle to move a
claim from draft to a paid/denied outcome, and a dashboard **Billing** menu to see and act on
claims. Submission to a real clearinghouse (Cvikota/Kareo/OfficeAlly-style) is stubbed behind an
interface so it can land later without touching this slice.

## Non-goals (this slice)

Real clearinghouse transport (HL7-DFT/SFTP, Kareo/OfficeAlly APIs), payments/ERA posting,
patient statements, claim scrubbing/validation rules, and resubmission/appeals. These are
**deferred, not designed away** — the `IClaimSubmitter` seam (§5) is their extension point.
See §9.

## Relationship to the existing `Billing` module

The repo already has `Modules.Billing` — that is **SaaS subscription billing** (Plans,
Subscriptions, Invoices, Usage snapshots: billing the *tenant/clinic* for using the software).
This module is **medical-claims billing** (billing an *insurance payer* for procedures performed
on a patient). They are different bounded contexts and must not be conflated. This slice does
**not** modify `Modules.Billing`. The backend context is named **`Claims`** to avoid a namespace
collision; the user-facing dashboard menu is labelled **"Billing"** because that is the word the
clinic uses and what the super bill links into.

## Architecture & boundaries

- New projects: `src/Modules/Claims/Modules.Claims` (runtime) + `Modules.Claims.Contracts`
  (its only public API). Implements `IModule` with
  `[assembly: FshModule(typeof(ClaimsModule), <order>)]`, where `<order>` is the next unused
  module order (the existing `Billing` module uses `500`) — confirm the free value against the
  other `FshModule` attributes when scaffolding.
- Registered in **all four** required places: `Host/.../Program.cs` Mediator `o.Assemblies`
  (two markers — runtime + Contracts) **and** `moduleAssemblies`, and the identical pair in
  `DbMigrator/Program.cs`. (Golden rule #2 — a missing Mediator marker = handlers silently
  undiscovered.)
- Owns `ClaimsDbContext` (subclasses `BaseDbContext`; tenant isolation default-ON;
  `base.OnModelCreating` called **last**). Migrations live in a new per-module folder
  `Host/FSH.Starter.Migrations.PostgreSQL/Claims`.
- **Only inbound coupling** is subscribing to `SuperBillSavedIntegrationEvent`
  (`FSH.Modules.Patient.Contracts.Events`) via `IIntegrationEventHandler<>`. No cross-module
  FKs. Everything the claim needs is a **snapshot** — mirrors the existing SuperBill /
  `SuperBillProcedure` "snapshot, not FK" rule.
- **No cross-module reads in the handler.** The event carries everything the claim snapshots
  (ids + code/description/charge + diagnostic ids). Display names (patient, payer, diagnosis
  text) are resolved **client-side** on the dashboard from data it already loads — the same
  pattern the super-bill insurance-snapshot plan established.

## Domain model

### `Claim` (aggregate root)

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | PK |
| `SuperBillId` | `Guid` | **Unique** — idempotency key (one claim per super bill) |
| `ReportId` | `Guid` | Snapshot from event |
| `PatientId` | `Guid` | Snapshot from event |
| `InsuranceTypeId` | `Guid?` | Snapshot from event (payer/insurance-type at billing time) |
| `Status` | `ClaimStatus` | See lifecycle below |
| `TotalCharge` | `decimal` | Derived = Σ line charges at snapshot time |
| `SubmittedAtUtc` | `DateTime?` | Set on `Submit` |
| `ResolvedAtUtc` | `DateTime?` | Set on `MarkPaid` / `MarkDenied` |
| `ControlNumber` | `string?` | Set by the submitter seam (stub records a fake one) |
| `CreatedAtUtc` / `UpdatedAtUtc` | `DateTime` | Audit |

### `ClaimLine` (child, owned)

Snapshot of one `ReportProcedureItem`: `ProcedureCodeId`, `Code`, `Description?`, `Charge`,
`DiagnosticIds` (stored as a snapshot list — e.g. `jsonb`/owned collection; exact mapping decided
in the plan).

### Lifecycle — `ClaimStatus`

```text
Draft ──► Ready ──► Submitted ──► Paid
                             └──► Denied
(any non-terminal) ──► Voided
```

Transitions are **guarded methods** on the aggregate: `MarkReady()`, `Submit(controlNumber)`,
`MarkPaid()`, `MarkDenied()`, `Void()`. An illegal transition throws a domain exception. Terminal
states: `Paid`, `Denied`, `Voided`.

## Data flow & idempotency

`SuperBillSavedIntegrationEvent` fires → `SuperBillSavedIntegrationEventHandler` **upserts** a
claim keyed by `SuperBillId`:

- **No existing claim** → create a `Draft` claim, snapshot lines + insurance type + total.
- **Existing claim still `Draft`** → refresh the snapshot in place (lines/insurance/total) so an
  edited super bill stays in sync before anyone has worked it.
- **Existing claim past `Draft`** (Ready/Submitted/Paid/Denied/Voided) → **do not mutate**; log
  and skip. This is the "freeze once billing has started" behaviour the super-bill
  insurance-snapshot plan's Task 9 anticipated — a biller's downstream work is never clobbered by
  a late chart edit.

The handler is **idempotent** (safe on event redelivery): the upsert-by-`SuperBillId` and the
past-Draft skip together guarantee no duplicates and no surprise mutations.

## Backend features (vertical slices)

Each command handler and each paginated query handler ships a `{Name}Validator` (golden rule #8).
Handlers are `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)` every await, and
propagate `CancellationToken`.

| Feature | Kind | Notes |
|---|---|---|
| `GetClaims` | paginated query | Worklist: filter by status/payer, search patient; returns rows + KPI counts (Draft/Ready/Submitted/Outstanding $). Has a validator. |
| `GetClaimById` | query | Claim + lines for the detail page. |
| `MarkClaimReady` | command | `Draft → Ready`. |
| `SubmitClaim` | command | `Ready → Submitted`; calls `IClaimSubmitter`. |
| `MarkClaimPaid` | command | `Submitted → Paid`. |
| `MarkClaimDenied` | command | `Submitted → Denied`. |
| `VoidClaim` | command | any non-terminal `→ Voided`. |

### Submission seam — `IClaimSubmitter`

`SubmitClaim` depends on an injected `IClaimSubmitter`. This slice ships **`StubClaimSubmitter`**:
marks the claim `Submitted` and records a synthetic `ControlNumber`. Real Cvikota/Kareo/OfficeAlly
submitters are later slices behind the **same interface** — no changes to this slice's handlers.

### Permissions

`Permissions.Claims.View` and `Permissions.Claims.Manage`, registered by the module
(`PermissionConstants.Register`, same pattern as `BillingModule`). `View` gates read/worklist;
`Manage` gates every transition command.

## Frontend — dashboard "Billing" menu

Design direction: **build on the dashboard's existing shadcn/Radix/Tailwind v4 system** (already
modern; zero legacy-BackChart lineage → no copyright risk) and give the Billing section its own
**teal accent** so it reads as its own space without forking the design system. The
**frontend-design skill drives the visual polish** during implementation (accent, status-pill
palette, spacing, empty/loading states) — the "UI skills" requested.

- **Nav** — new section in
  [nav-data.ts](../../../clients/dashboard/src/components/layout/nav-data.ts):
  `id: "billing"`, caption **"Billing"**, one item **Claims** → `/billing/claims`
  (icon e.g. `ReceiptText`), gated `perm: "Permissions.Claims.View"`.
- **Worklist** (`pages/billing/claims-list.tsx`) — **table + KPI summary strip** (chosen
  layout): summary tiles (Draft / Ready / Submitted / Outstanding $) above a filterable, sortable
  table (patient, payer, CPT count, charge, status pill). Patient & payer **names resolved
  client-side** from the already-loaded `patients` list + `useInsuranceTypeOptions` — no new
  backend joins.
- **Detail** (`pages/billing/claim-detail.tsx`) — **two-column + sticky action bar** (chosen
  layout): left column = procedures (snapshot) + diagnoses + link to source report; right rail =
  payer + activity; lifecycle buttons pinned bottom-right, each a TanStack mutation passing
  per-call data through `mutate(arg)` (golden rule #9). Buttons enable/disable to mirror the
  aggregate's legal transitions.
- **API module** `src/api/claims.ts` — hand-written `apiFetch` (no codegen). Lazy route
  registration for both pages.

## Testing

- **Backend** (`src/Tests/Claims.Tests`, xUnit + Shouldly + NSubstitute):
  - Event handler creates a `Draft` claim and snapshots lines/insurance/total.
  - Re-save while `Draft` refreshes the snapshot.
  - Re-save past `Draft` is **skipped** (freeze) — claim unchanged.
  - Handler is idempotent on redelivery (no duplicate claim for the same `SuperBillId`).
  - Each transition command enforces legal moves and rejects illegal ones.
  - Validators reject empty ids / bad paging.
  - `StubClaimSubmitter` marks the claim `Submitted` and sets a control number.
- **Architecture.Tests** stays green (module-boundary + validator-presence rules).
- **Dashboard Playwright** (route-mocked, no vitest/jest): worklist renders and filters by
  status; opening a claim shows the snapshot; **Mark Ready → Submit** advances the pill and calls
  the correct endpoint.

## Registration checklist (golden rule #2)

- [ ] `Host/FSH.Starter.Api/Program.cs` — add Claims runtime + Contracts markers to Mediator
      `o.Assemblies`.
- [ ] Same file — add Claims assemblies to the `moduleAssemblies` array.
- [ ] `Host/FSH.Starter.DbMigrator/Program.cs` — add the **identical** pair.
- [ ] New migration folder `Migrations.PostgreSQL/Claims` + initial migration against
      `ClaimsDbContext`.

## Out of scope / later slices (§9)

Real clearinghouse transport (HL7-DFT/SFTP, Kareo/OfficeAlly API submitters behind
`IClaimSubmitter`); payments/ERA posting; patient statements; claim scrubbing/validation rules;
resubmission/appeals; diagnosis **descriptions** beyond ids-resolved-on-dashboard. Approved to be
added in follow-up slices.

## Docs & changelog (golden rule #10)

A user-facing change lands here → update the separate docs repo
(`github.com/fullstackhero/docs`) and add a changelog entry
(`src/content/docs/changelog/`) when the feature ships. Tracked as part of implementation, noted
here so it isn't forgotten.
