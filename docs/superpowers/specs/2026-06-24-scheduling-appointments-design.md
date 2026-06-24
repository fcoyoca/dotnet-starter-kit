# Scheduling / Appointments — Design

## Context

The legacy **BackChart** scheduler (`BackChart/Pages/Schedules/Scheduler.razor`, route `/scheduler`,
a Syncfusion `EjsSchedule`) is its own top-level navigation area — not part of Administration. clinic-app
has migrated the Schedule **admin config** (per-clinic `ScheduleConfig` units + tenant `AppointmentType`
CRUD) into the Administration module, but there is **no operational appointment booking / calendar** yet.

This design adds that operational scheduler as a **new, dedicated `Scheduling` module** with its **own
dashboard navigation section** (mirroring legacy's separate "Schedules" menu). It covers full legacy parity,
delivered in phases:

- **Phase 1** — Core day scheduler: `Appointment` CRUD + status lifecycle, day view grouped by provider,
  booking/manage dialog, **SignalR live updates** (replacing the legacy 60s polling timer), multi-timezone.
- **Phase 2** — Reserved/recurring-daily time blocks, reschedule chains, week view.
- **Phase 3** — Email appointment reminders (+ confirm flow), month view.

Out of scope (no backing entities exist): rooms (`apptRoomID`), incident/case linkage (`apptIncidentID`),
Kareo/external sync (dropped per the migration plan). SMS reminders deferred (no SMS provider) — the reminder
sender is an interface so SMS can slot in later.

## Why a separate module (decision)

The user requires the scheduler under its **own navigation**, distinct from Administration. A dedicated
`Scheduling` bounded context fits: appointments are operational/transactional, whereas Administration holds
config master-data (clinics, providers, appointment types, schedule units).

**Consequence (golden rule #1 — modules reference each other only via `.Contracts`, never runtime, and
never via cross-module DB FKs):** the appointment's relationships become **id-refs**, not same-module FKs:

| Field | Owning module | Validated / enriched via (already exists) |
|---|---|---|
| `ClinicId` | Administration | `GetClinicByIdQuery`, `ClinicDto` (carries `TimeZoneId`, added here) |
| `ProviderId` | Administration | `GetProviderByIdQuery`, `ListProvidersQuery` |
| `AppointmentTypeId?` | Administration | `ListAppointmentTypesQuery`, `AppointmentTypeDto` (color, duration) |
| (schedule units) | Administration | `GetScheduleConfigQuery` (day start/end + interval per clinic) |
| `PatientId?` | Patient | `SearchPatientsQuery`, `GetPatientByIdQuery` |

The `Scheduling` runtime project references `Administration.Contracts` + `Patient.Contracts` (the same
`.Contracts`-only pattern Patient/Administration already use). **No patient PHI is duplicated into
Scheduling** — only `PatientId` is stored; names/DOB are resolved from the (PHI-encrypting) Patient module
for the visible appointments.

## Architecture

### New module: `src/Modules/Scheduling/`
- `Modules.Scheduling` (runtime) + `Modules.Scheduling.Contracts` (public API), mirroring an existing module.
- `SchedulingDbContext : BaseDbContext`, schema `scheduling`, `base.OnModelCreating` **last**, tenant-isolated.
- `IModule` with `[assembly: FshModule(typeof(SchedulingModule), <order>)]`; route group
  `api/v{version}/scheduling`.
- **Register in all FOUR places** (`add-module` skill): `Api/Program.cs` Mediator `o.Assemblies` (two markers:
  `SchedulingContractsMarker` + `SchedulingModule`) **and** `moduleAssemblies`; the **identical pair** in
  `DbMigrator/Program.cs`. New migration folder `Migrations.PostgreSQL/Scheduling/`.
- `.csproj` references: `Persistence`, `Web`, `Modules.Scheduling.Contracts`, `Administration.Contracts`,
  `Patient.Contracts`, and `Auditing.Contracts` (audit pattern).

### Domain — `Appointment` (`AggregateRoot<Guid>, ISoftDeletable`, tenant-scoped)
- Refs: `ClinicId`, `ProviderId`, `PatientId?` (null ⇒ reserved block), `AppointmentTypeId?`.
- Time: `StartUtc`, `EndUtc` (`timestamptz`). Guard `EndUtc > StartUtc`.
- Status: enum `AppointmentStatus { Scheduled, CheckedIn, CheckedOut }` + flags `Cancelled`, `NoShow`.
  Explicit transition methods: `CheckIn()`, `CheckOut()`, `Cancel()`, `MarkNoShow()`, `Reschedule(newId)`.
- Blocks (Phase 2): `ReservedTimeTitle?` (set ⇒ block; mutually exclusive with `PatientId`), `IsRecurringDaily`.
- Reschedule (Phase 2): `RescheduledToId?` (self-ref id; cancel old + create linked new).
- Reminders (Phase 3): `ReminderSentUtc?`, `ReminderConfirmedUtc?`, `ReminderToken?`.
- `Notes?`, `LegacyId?` (legacy `apptID` for a future import).
- EF config: indexes on `(ClinicId, StartUtc)`, `(ProviderId, StartUtc)`, `PatientId`, `IsDeleted`,
  `LegacyId`. No FK constraints to other modules' tables.

### One change to an existing entity: `Clinic.TimeZoneId`
- Add `Clinic.TimeZoneId` (IANA string, e.g. `America/New_York`) in **Administration** with a small migration;
  surface on `ClinicDto` + the clinic create/edit form (default to a sensible tenant zone). This is the only
  edit outside the new module.

### Multi-timezone strategy
- Store UTC `timestamptz`; API speaks UTC ISO-8601 (`…Z`). The dashboard renders the grid + appointments in
  the **selected clinic's** `TimeZoneId` (`Intl`/`date-fns-tz`) and converts a clicked slot → UTC on save.
- `ScheduleConfig` start/end stay clinic-local wall-clock (`TimeOnly`). No server-side local-time math —
  conversion lives only at the dashboard edge. DST-safe across any number of clinic zones.

### API (`/api/v1/scheduling`, every command + the range query gets a validator)
- `GET /appointments?clinicId&providerIds&fromUtc&toUtc` → appointments in range (the calendar load).
- `GET /appointments/{id}`, `POST /appointments`, `PUT /appointments/{id}`, `DELETE /appointments/{id}`.
- Lifecycle: `POST /appointments/{id}/{check-in|check-out|cancel|no-show|confirm}`,
  `POST /appointments/{id}/reschedule` (Phase 2).
- Handlers validate refs via Mediator to the contracts above (clinic/provider/type/patient existence);
  create checks the slot is inside the clinic's `ScheduleConfig` window (soft validation).
- Read-model enrichment: the range query returns ids + times + status + type color/name + provider name
  (Administration via Mediator/batch) and `PatientId`; **patient display name/DOB are resolved by the
  dashboard** from the Patient API for the visible ids. If an efficient batch is needed, add a small
  `GetPatientsByIdsQuery` (id → display name, DOB) to `Patient.Contracts` (keeps PHI in Patient).

### Realtime — replaces the 60s poller
- On every appointment mutation, the handler broadcasts `"AppointmentChanged"` to group `tenant:{tenantId}`
  via `IHubContext<AppHub>` (`src/BuildingBlocks/Web/Realtime/`), payload
  `{ clinicId, providerId, startUtc, endUtc, action }`.
- The dashboard scheduler page subscribes with the existing `useRealtimeEvent("AppointmentChanged")`
  (`clients/dashboard/src/realtime/realtime-context.tsx`) and invalidates/refetches the affected day. No timer.

### Reminders (Phase 3)
- Hangfire recurring job scans upcoming appointments needing a reminder, sends **email** via the existing
  Mailing/Notifications stack with a confirm link carrying `ReminderToken`; `POST .../confirm` stamps
  `ReminderConfirmedUtc`. Sender is behind an interface (`IAppointmentReminderSender`) so SMS can be added.

### Dashboard — new navigation + calendar
- New **top-level nav section** "Schedule" (e.g. `CalendarClock` icon) → route `/scheduler`
  (`clients/dashboard/src/components/layout/nav-data.ts` `sections[]` + `routes.tsx` lazy route), gated on
  `Scheduling.Appointments.View`.
- New `clients/dashboard/src/api/scheduling.ts` facade (appointment CRUD + lifecycle + range query).
- New `clients/dashboard/src/pages/scheduler/` calendar (hand-rolled Radix/Tailwind, **no Syncfusion**):
  filter bar **Clinic → Provider(s) → view**; **Day** (provider-column resource grid — primary, Phase 1),
  **Week** (Phase 2), **Month** (Phase 3). Click empty slot → book; click event → manage dialog.
- Booking/manage dialog: patient search (Patient `SearchPatients`) **or** reserved-title; clinic / provider /
  type / date / time / duration / notes; lifecycle buttons (check-in/out, cancel, no-show, reschedule,
  confirm); patient contact panel. Times shown in the clinic's zone.

### Permissions
- New `Scheduling.Appointments` resource: `View` (IsBasic) / `Create` / `Update` / `Delete`, plus a
  `Scheduling.Schedule.Edit` gate mirroring legacy `SCHEDULEEDIT` for the lifecycle actions. Registered via
  `PermissionConstants.Register(...)` in the module; nav + RouteGuard mirror `View`.

## Phasing (each phase ships end-to-end: backend slice → migration → tests → dashboard → docs)

1. **Phase 1 — Core + realtime + timezone.** `Scheduling` module scaffold + 4-place registration;
   `Appointment` entity; `Clinic.TimeZoneId`; CRUD + status lifecycle endpoints; SignalR broadcast +
   listener; Day view + booking dialog; permissions; migrations. *Bulk of the value.*
2. **Phase 2 — Blocks + reschedule + week view.** Reserved/recurring-daily blocks; reschedule chains;
   week view.
3. **Phase 3 — Reminders + month view.** Hangfire email reminder job + confirm endpoint/flow; month view.

## Testing
- **Unit** (`src/Tests/Scheduling.Tests`): domain transitions (check-in/out/cancel/no-show/reschedule),
  `EndUtc > StartUtc`, patient-XOR-reserved-title; command + range-query validators.
- **Integration** (`src/Tests/Integration.Tests/Tests/Scheduling/`, Testcontainers/Postgres): range query,
  lifecycle, tenant isolation, cross-module ref validation.
- **Architecture.Tests**: new module classifies tenant-isolated; validators present; boundaries respected.
- **Dashboard**: tsc + eslint clean; Playwright route-mocked test for book + live-update via
  `AppointmentChanged`.

## Verification (per phase)
- `dotnet build src/FSH.Starter.slnx` (0 warn). `dotnet test` (unit + Architecture; integration needs Docker).
- `dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply` → confirm `scheduling.Appointments` +
  `Clinic.TimeZoneId`; exercise `/api/v1/scheduling/appointments` via Scalar with a tenant header.
- Dashboard: book an appointment in one browser tab, confirm it appears live in a second tab (SignalR), and
  that a Manila clinic vs a New York clinic each render their own local day.

## Open follow-ups
- Docs + changelog in the separate `fullstackhero/docs` repo (golden rule #10).
- Optional `GetPatientsByIdsQuery` batch in `Patient.Contracts` if per-id resolution proves chatty.
