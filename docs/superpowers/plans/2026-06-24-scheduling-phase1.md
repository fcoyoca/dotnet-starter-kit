# Scheduling Phase 1 (Core + Realtime + Timezone) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a working, modern, realtime day-scheduler — a new `Scheduling` module with its own dashboard nav (`/scheduler`), `Appointment` CRUD + status lifecycle, day view grouped by provider, booking dialog, and SignalR live updates replacing the legacy 60s poller — multi-timezone per clinic.

**Architecture:** New modular-monolith bounded context `src/Modules/Scheduling` (runtime + `.Contracts`), tenant-isolated `SchedulingDbContext` (schema `scheduling`). `Appointment` stores `ClinicId`/`ProviderId`/`AppointmentTypeId`/`PatientId` as **id-refs** (no cross-module FKs); existence is validated via the other modules' `.Contracts` queries sent through Mediator. Times are stored UTC (`timestamptz`) and rendered in the selected clinic's IANA timezone by the dashboard. Mutations broadcast `AppointmentChanged` to the SignalR `tenant:{tenantId}` group.

**Tech Stack:** .NET 10, EF Core 10 / PostgreSQL, Mediator 3.x (source-gen), FluentValidation, SignalR `AppHub`, React 19 + Vite + TanStack Query v5 + Radix/Tailwind, `date-fns` / `date-fns-tz`, `@microsoft/signalr` (dashboard already depends on it).

## Global Constraints

- Mediator handlers: `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)` every await, propagate `CancellationToken`. (`.agents/rules/api-conventions.md`)
- Every command handler + the paginated/range query handler needs a `{Name}Validator` (enforced by `Architecture.Tests`).
- Tenant isolation default-ON via `BaseDbContext`; `Appointment` is NOT `IGlobalEntity`. Subclass DbContext calls `base.OnModelCreating` **last**.
- Module registration touches FOUR places: `Api/Program.cs` Mediator `o.Assemblies` (TWO markers) + `moduleAssemblies`, and the identical pair in `DbMigrator/Program.cs`. A missing Mediator marker = handlers silently undiscovered.
- Do NOT modify `src/BuildingBlocks` (use `IHubContext<AppHub>` from `FSH.Framework.Web.Realtime`, don't change it).
- Build runs `TreatWarningsAsErrors`; SonarAnalyzer S1144/S3604/S2325/CA1822/S4581 fail the build. Migrations: hand-fix any scaffolded `new Guid("…")` (S4581).
- Backend coding style: file-scoped namespaces, 4-space indent, `is null`, records for DTOs/commands, `default!` for required strings, `Guid.CreateVersion7()` for new ids.
- Dashboard: `cn` util is `@/lib/cn` (NOT `@/lib/utils`); hand-written `apiFetch`; per-call data via `mutate(arg)`; no react-hook-form/zod in dashboard.
- API speaks UTC ISO-8601 (`…Z`); no server-side local-time math. System.Text.Json serializes enums as string names (mirror as TS string unions).
- Branch: `clinic-app`. One commit per task.

## Reference templates (existing files to clone — read before each task)

- Module shape / registration: `.agents/skills/add-module/SKILL.md`; `src/Modules/Administration/Modules.Administration/AdministrationModule.cs`; `src/Modules/Administration/Modules.Administration/Data/AdministrationDbContext.cs`.
- Tenant aggregate + EF config: `src/Modules/Administration/Modules.Administration/Domain/AppointmentType.cs` + `Data/Configurations/AppointmentTypeConfiguration.cs`.
- CRUD slice (handler/validator/endpoint): `src/Modules/Administration/Modules.Administration/Features/v1/AppointmentTypes/*`.
- Non-paginated list query: `.../AppointmentTypes/ListAppointmentTypes/*`.
- Permissions: `src/Modules/Administration/Modules.Administration.Contracts/Authorization/AdministrationPermissions.cs`.
- Contracts marker: `src/Modules/Administration/Modules.Administration.Contracts/AdministrationContractsMarker.cs` (find via grep).
- Realtime backend: `.agents/rules/realtime.md` (AppHub, `IHubContext<AppHub>`, `tenant:{id}` group).
- Dashboard realtime client: `clients/dashboard/src/realtime/realtime-context.tsx` (`useRealtimeEvent<T>(name)`).
- Dashboard facade + page + nav + route: `clients/dashboard/src/api/administration.ts`; `clients/dashboard/src/pages/administration/schedule.tsx`; `clients/dashboard/src/components/layout/nav-data.ts`; `clients/dashboard/src/routes.tsx`.
- Patient search contract: `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/SearchPatientsQuery.cs`, `GetPatientByIdQuery.cs`.

---

## Task 1: Scaffold the `Scheduling` module (buildable empty module, registered in 4 places)

**Files:**
- Create: `src/Modules/Scheduling/Modules.Scheduling.Contracts/Modules.Scheduling.Contracts.csproj`
- Create: `src/Modules/Scheduling/Modules.Scheduling.Contracts/SchedulingContractsMarker.cs`
- Create: `src/Modules/Scheduling/Modules.Scheduling/Modules.Scheduling.csproj`
- Create: `src/Modules/Scheduling/Modules.Scheduling/SchedulingModule.cs`
- Create: `src/Modules/Scheduling/Modules.Scheduling/Data/SchedulingDbContext.cs`
- Modify: `src/Host/FSH.Starter.Api/Program.cs` (Mediator `o.Assemblies` + `moduleAssemblies`)
- Modify: `src/Host/FSH.Starter.DbMigrator/Program.cs` (identical pair)
- Modify: `src/FSH.Starter.slnx` (add both projects)

**Interfaces:**
- Produces: `FSH.Modules.Scheduling.Contracts.SchedulingContractsMarker`, `FSH.Modules.Scheduling.SchedulingModule`, `FSH.Modules.Scheduling.Data.SchedulingDbContext` (schema `scheduling`).

- [ ] **Step 1: Read the scaffolder recipe and a reference module**

Read `.agents/skills/add-module/SKILL.md`, `AdministrationModule.cs`, `AdministrationDbContext.cs`, and `Modules.Administration.csproj`. Note the order value of the highest existing `[assembly: FshModule(... , N)]` (Administration is 800) — pick the next free order (e.g. **810**).

- [ ] **Step 2: Create the two `.csproj` files**

`Modules.Scheduling.Contracts.csproj` — mirror `Modules.Administration.Contracts.csproj` (find it, same `<ProjectReference>`s to shared contracts/abstractions only).

`Modules.Scheduling.csproj` — mirror `Modules.Administration/Modules.Administration.csproj` plus these references so the runtime can consume other modules' contracts:

```xml
<ItemGroup>
  <ProjectReference Include="..\..\..\BuildingBlocks\Persistence\Persistence.csproj" />
  <ProjectReference Include="..\..\..\BuildingBlocks\Web\Web.csproj" />
  <ProjectReference Include="..\Modules.Scheduling.Contracts\Modules.Scheduling.Contracts.csproj" />
  <ProjectReference Include="..\..\Administration\Modules.Administration.Contracts\Modules.Administration.Contracts.csproj" />
  <ProjectReference Include="..\..\Patient\Modules.Patient.Contracts\Modules.Patient.Contracts.csproj" />
  <ProjectReference Include="..\..\Auditing\Modules.Auditing.Contracts\Modules.Auditing.Contracts.csproj" />
</ItemGroup>
```

- [ ] **Step 3: Create the marker + DbContext + module**

`SchedulingContractsMarker.cs`:
```csharp
namespace FSH.Modules.Scheduling.Contracts;

/// <summary>Assembly marker for Mediator discovery of the Scheduling contracts.</summary>
public static class SchedulingContractsMarker;
```

`Data/SchedulingDbContext.cs` — mirror `AdministrationDbContext.cs` exactly, renaming, `Schema = "scheduling"`, no DbSets yet:
```csharp
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Scheduling.Data;

public sealed class SchedulingDbContext : BaseDbContext
{
    public const string Schema = "scheduling";

    public SchedulingDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<SchedulingDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

`SchedulingModule.cs` — mirror `AdministrationModule.cs` `ConfigureServices`/`ConfigureMiddleware`/`MapEndpoints` skeleton (empty `MapEndpoints` group `api/v{version:apiVersion}/scheduling`, `WithTags("Scheduling")`, `RequireAuthorization()`), register `AddHeroDbContext<SchedulingDbContext>()`, health check `db:scheduling`, and `[assembly: FshModule(typeof(SchedulingModule), 810)]`. Leave `PermissionConstants.Register(...)` commented until Task 8.

- [ ] **Step 4: Register the module in all four places**

In `src/Host/FSH.Starter.Api/Program.cs` add to `o.Assemblies` (after the Administration pair):
```csharp
typeof(FSH.Modules.Scheduling.Contracts.SchedulingContractsMarker),
typeof(FSH.Modules.Scheduling.SchedulingModule),
```
and to `moduleAssemblies`:
```csharp
typeof(FSH.Modules.Scheduling.SchedulingModule).Assembly,
```
Make the **identical** two edits in `src/Host/FSH.Starter.DbMigrator/Program.cs`. Add both projects to `src/FSH.Starter.slnx`.

- [ ] **Step 5: Build**

Run: `dotnet build src/FSH.Starter.slnx -clp:ErrorsOnly`
Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`.

- [ ] **Step 6: Commit**
```bash
git add -A && git commit -m "feat(scheduling): scaffold Scheduling module + register in 4 places"
```

---

## Task 2: Add `Clinic.TimeZoneId` (Administration)

**Files:**
- Modify: `src/Modules/Administration/Modules.Administration/Domain/Clinic.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Data/Configurations/ClinicConfiguration.cs`
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/ClinicDto.cs`
- Modify: Create/Update clinic commands + handlers + validators under `.../Features/v1/Clinics/*`
- Modify: `clients/dashboard/src/api/administration.ts` (ClinicDto + create/update payloads), `clients/dashboard/src/pages/administration/clinic-detail.tsx` (timezone field)
- Test: `src/Tests/Administration.Tests/Domain/ClinicTests.cs` (add cases)
- Migration: `Migrations.PostgreSQL/Administration/`

**Interfaces:**
- Produces: `Clinic.TimeZoneId` (string, IANA, non-null, default `"UTC"`); `ClinicDto.TimeZoneId`.

- [ ] **Step 1: Write the failing domain test**

Add to `ClinicTests.cs`:
```csharp
[Fact]
public void Create_Should_DefaultTimeZone_To_Utc_When_Blank()
{
    var c = Clinic.Create("C1", "Main", "1 St", null, "City", "ST", "00000", null, timeZoneId: null);
    c.TimeZoneId.ShouldBe("UTC");
}

[Fact]
public void Update_Should_SetTimeZone()
{
    var c = Clinic.Create("C1", "Main", "1 St", null, "City", "ST", "00000", null);
    c.Update("C1", "Main", "1 St", null, "City", "ST", "00000", null, isActive: true, timeZoneId: "America/New_York");
    c.TimeZoneId.ShouldBe("America/New_York");
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~ClinicTests"`
Expected: FAIL (compile error — `timeZoneId` param/property missing).

- [ ] **Step 3: Add the property + params**

In `Clinic.cs` add `public string TimeZoneId { get; private set; } = "UTC";`. Add a trailing `string? timeZoneId = null` param to `Create(...)` and `Update(...)`; set `TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "UTC" : timeZoneId.Trim();`. In `ClinicConfiguration.cs` add `builder.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(64);`. Add `TimeZoneId` to `ClinicDto` and to the Create/Update commands + handlers + validators (validator: `NotEmpty().MaximumLength(64)`); GetById/List projections include it.

- [ ] **Step 4: Run tests**

Run: `dotnet test src/Tests/Administration.Tests/Administration.Tests.csproj --filter "FullyQualifiedName~ClinicTests"`
Expected: PASS.

- [ ] **Step 5: Dashboard field**

In `administration.ts` add `timeZoneId` to `ClinicDto`, `ClinicInput`, and the create/update bodies. In `clinic-detail.tsx` add a timezone `<select>` (a short curated IANA list: `UTC`, `America/New_York`, `America/Chicago`, `America/Denver`, `America/Los_Angeles`, `Asia/Manila`, …) defaulting to `UTC`.

- [ ] **Step 6: Migration + build + commit**

Run:
```bash
dotnet ef migrations add AddClinicTimeZone --project src/Host/FSH.Starter.Migrations.PostgreSQL --startup-project src/Host/FSH.Starter.Api --context AdministrationDbContext
dotnet build src/FSH.Starter.slnx -clp:ErrorsOnly
```
Hand-check the migration sets a non-null default `'UTC'` for existing rows. Then:
```bash
git add -A && git commit -m "feat(administration): Clinic.TimeZoneId for scheduler multi-timezone"
```

---

## Task 3: `Appointment` domain entity + unit tests

**Files:**
- Create: `src/Modules/Scheduling/Modules.Scheduling/Domain/AppointmentStatus.cs`
- Create: `src/Modules/Scheduling/Modules.Scheduling/Domain/Appointment.cs`
- Create: `src/Tests/Scheduling.Tests/Scheduling.Tests.csproj` (mirror `src/Tests/Administration.Tests/Administration.Tests.csproj`; reference `Modules.Scheduling`)
- Create: `src/Tests/Scheduling.Tests/Domain/AppointmentTests.cs`
- Modify: `src/FSH.Starter.slnx` (add test project)

**Interfaces:**
- Produces:
  - `enum AppointmentStatus { Scheduled, CheckedIn, CheckedOut }`
  - `Appointment.Create(Guid clinicId, Guid providerId, Guid? patientId, Guid? appointmentTypeId, DateTime startUtc, DateTime endUtc, string? notes, int? legacyId = null)`
  - `Appointment.Update(Guid clinicId, Guid providerId, Guid? patientId, Guid? appointmentTypeId, DateTime startUtc, DateTime endUtc, string? notes)`
  - `CheckIn()`, `CheckOut()`, `Cancel()`, `MarkNoShow()`, `Delete(string? deletedBy)`
  - Props: `ClinicId, ProviderId, PatientId?, AppointmentTypeId?, StartUtc, EndUtc, Notes?, Status, Cancelled, NoShow, LegacyId?, CreatedAtUtc, UpdatedAtUtc?, IsDeleted, DeletedOnUtc?, DeletedBy?` (all `{ get; private set; }`).

- [ ] **Step 1: Write the failing tests**

`src/Tests/Scheduling.Tests/Domain/AppointmentTests.cs`:
```csharp
using FSH.Modules.Scheduling.Domain;

namespace Scheduling.Tests.Domain;

public sealed class AppointmentTests
{
    private static Appointment New() => Appointment.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
        new DateTime(2026, 6, 24, 14, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 6, 24, 14, 30, 0, DateTimeKind.Utc), "  hi  ");

    [Fact]
    public void Create_Should_Set_Defaults_And_TrimNotes()
    {
        var a = New();
        a.Id.ShouldNotBe(Guid.Empty);
        a.Status.ShouldBe(AppointmentStatus.Scheduled);
        a.Cancelled.ShouldBeFalse();
        a.NoShow.ShouldBeFalse();
        a.Notes.ShouldBe("hi");
        a.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_Throw_When_EndNotAfterStart()
    {
        var t = new DateTime(2026, 6, 24, 14, 0, 0, DateTimeKind.Utc);
        Should.Throw<ArgumentException>(() =>
            Appointment.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null, null, t, t, null));
    }

    [Fact]
    public void Lifecycle_CheckIn_Then_CheckOut_Advances_Status()
    {
        var a = New();
        a.CheckIn();
        a.Status.ShouldBe(AppointmentStatus.CheckedIn);
        a.CheckOut();
        a.Status.ShouldBe(AppointmentStatus.CheckedOut);
    }

    [Fact]
    public void Cancel_And_NoShow_Set_Flags()
    {
        var a = New();
        a.Cancel();
        a.Cancelled.ShouldBeTrue();
        var b = New();
        b.MarkNoShow();
        b.NoShow.ShouldBeTrue();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var a = New();
        a.Delete("admin@tenant");
        a.IsDeleted.ShouldBeTrue();
        a.DeletedOnUtc.ShouldNotBeNull();
        a.DeletedBy.ShouldBe("admin@tenant");
    }
}
```

- [ ] **Step 2: Run to verify it fails**

Run: `dotnet test src/Tests/Scheduling.Tests/Scheduling.Tests.csproj`
Expected: FAIL (types not defined).

- [ ] **Step 3: Implement the entity**

`Domain/AppointmentStatus.cs`:
```csharp
namespace FSH.Modules.Scheduling.Domain;

public enum AppointmentStatus { Scheduled, CheckedIn, CheckedOut }
```

`Domain/Appointment.cs` (mirror `AppointmentType.cs` conventions):
```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Scheduling.Domain;

/// <summary>
/// A booked appointment (legacy <c>Appointments</c>). Tenant-scoped; references Clinic/Provider/AppointmentType
/// (Administration) and Patient (Patient) by id only — no cross-module FKs. Times are UTC instants.
/// </summary>
public sealed class Appointment : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid ClinicId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid? PatientId { get; private set; }
    public Guid? AppointmentTypeId { get; private set; }
    public DateTime StartUtc { get; private set; }
    public DateTime EndUtc { get; private set; }
    public string? Notes { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public bool Cancelled { get; private set; }
    public bool NoShow { get; private set; }
    public int? LegacyId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Appointment() { }

    public static Appointment Create(
        Guid clinicId, Guid providerId, Guid? patientId, Guid? appointmentTypeId,
        DateTime startUtc, DateTime endUtc, string? notes, int? legacyId = null)
    {
        Guard(clinicId, providerId, startUtc, endUtc);
        return new Appointment
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            ProviderId = providerId,
            PatientId = patientId,
            AppointmentTypeId = appointmentTypeId,
            StartUtc = startUtc,
            EndUtc = endUtc,
            Notes = Trim(notes),
            Status = AppointmentStatus.Scheduled,
            LegacyId = legacyId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(Guid clinicId, Guid providerId, Guid? patientId, Guid? appointmentTypeId,
        DateTime startUtc, DateTime endUtc, string? notes)
    {
        Guard(clinicId, providerId, startUtc, endUtc);
        ClinicId = clinicId;
        ProviderId = providerId;
        PatientId = patientId;
        AppointmentTypeId = appointmentTypeId;
        StartUtc = startUtc;
        EndUtc = endUtc;
        Notes = Trim(notes);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CheckIn() { Status = AppointmentStatus.CheckedIn; UpdatedAtUtc = DateTime.UtcNow; }
    public void CheckOut() { Status = AppointmentStatus.CheckedOut; UpdatedAtUtc = DateTime.UtcNow; }
    public void Cancel() { Cancelled = true; UpdatedAtUtc = DateTime.UtcNow; }
    public void MarkNoShow() { NoShow = true; UpdatedAtUtc = DateTime.UtcNow; }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    private static void Guard(Guid clinicId, Guid providerId, DateTime startUtc, DateTime endUtc)
    {
        if (clinicId == Guid.Empty) throw new ArgumentException("Clinic is required.", nameof(clinicId));
        if (providerId == Guid.Empty) throw new ArgumentException("Provider is required.", nameof(providerId));
        if (endUtc <= startUtc) throw new ArgumentException("End must be after start.", nameof(endUtc));
    }

    private static string? Trim(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test src/Tests/Scheduling.Tests/Scheduling.Tests.csproj`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**
```bash
git add -A && git commit -m "feat(scheduling): Appointment domain entity + unit tests"
```

---

## Task 4: EF config, DbSet, and `AddAppointments` migration

**Files:**
- Create: `src/Modules/Scheduling/Modules.Scheduling/Data/Configurations/AppointmentConfiguration.cs`
- Modify: `src/Modules/Scheduling/Modules.Scheduling/Data/SchedulingDbContext.cs` (add DbSet)
- Migration: `src/Host/FSH.Starter.Migrations.PostgreSQL/Scheduling/`

**Interfaces:**
- Consumes: `Appointment` (Task 3), `SchedulingDbContext` (Task 1).
- Produces: `SchedulingDbContext.Appointments` (`DbSet<Appointment>`), table `scheduling.Appointments`.

- [ ] **Step 1: Write the EF config** (mirror `AppointmentTypeConfiguration.cs`)

```csharp
using FSH.Modules.Scheduling.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Scheduling.Data.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Appointments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClinicId).IsRequired();
        builder.Property(x => x.ProviderId).IsRequired();
        builder.Property(x => x.StartUtc).IsRequired();
        builder.Property(x => x.EndUtc).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Property(x => x.LegacyId);

        builder.HasIndex(x => new { x.ClinicId, x.StartUtc });
        builder.HasIndex(x => new { x.ProviderId, x.StartUtc });
        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.LegacyId);

        builder.Ignore(x => x.DomainEvents);
    }
}
```

- [ ] **Step 2: Add the DbSet**

In `SchedulingDbContext.cs` add: `public DbSet<Appointment> Appointments => Set<Appointment>();` (and `using FSH.Modules.Scheduling.Domain;`).

- [ ] **Step 3: Generate the migration**

Run:
```bash
dotnet ef migrations add AddAppointments --project src/Host/FSH.Starter.Migrations.PostgreSQL --startup-project src/Host/FSH.Starter.Api --context SchedulingDbContext
```
Expected: creates `Migrations.PostgreSQL/Scheduling/<ts>_AddAppointments.cs`. Open it; confirm `Status` is `text`, `StartUtc`/`EndUtc` are `timestamp with time zone`, the index on `(ClinicId, StartUtc, TenantId)` exists, and there are **no FK constraints** to other schemas.

- [ ] **Step 4: Build**

Run: `dotnet build src/FSH.Starter.slnx -clp:ErrorsOnly`
Expected: 0 errors.

- [ ] **Step 5: Commit**
```bash
git add -A && git commit -m "feat(scheduling): Appointment EF config + AddAppointments migration"
```

---

## Task 5: Contracts — DTO, commands, queries

**Files (all under `src/Modules/Scheduling/Modules.Scheduling.Contracts/`):**
- Create: `Dtos/AppointmentDto.cs`
- Create: `v1/Appointments/CreateAppointmentCommand.cs`, `UpdateAppointmentCommand.cs`, `DeleteAppointmentCommand.cs`, `GetAppointmentByIdQuery.cs`, `ListAppointmentsQuery.cs`
- Create: `v1/Appointments/{CheckInAppointmentCommand,CheckOutAppointmentCommand,CancelAppointmentCommand,NoShowAppointmentCommand}.cs`

**Interfaces:**
- Produces (consumed by Tasks 6–9 + dashboard):
```csharp
public sealed record AppointmentDto(
    Guid Id, Guid ClinicId, Guid ProviderId, Guid? PatientId, Guid? AppointmentTypeId,
    DateTime StartUtc, DateTime EndUtc, string? Notes, string Status, bool Cancelled, bool NoShow);

public sealed record ListAppointmentsQuery(
    Guid ClinicId, IReadOnlyList<Guid>? ProviderIds, DateTime FromUtc, DateTime ToUtc)
    : IQuery<IReadOnlyList<AppointmentDto>>;

public sealed record GetAppointmentByIdQuery(Guid Id) : IQuery<AppointmentDto>;

public sealed record CreateAppointmentCommand(
    Guid ClinicId, Guid ProviderId, Guid? PatientId, Guid? AppointmentTypeId,
    DateTime StartUtc, DateTime EndUtc, string? Notes) : ICommand<Guid>;

public sealed record UpdateAppointmentCommand(
    Guid Id, Guid ClinicId, Guid ProviderId, Guid? PatientId, Guid? AppointmentTypeId,
    DateTime StartUtc, DateTime EndUtc, string? Notes) : ICommand<Unit>;

public sealed record DeleteAppointmentCommand(Guid Id) : ICommand<Unit>;
public sealed record CheckInAppointmentCommand(Guid Id) : ICommand<Unit>;
public sealed record CheckOutAppointmentCommand(Guid Id) : ICommand<Unit>;
public sealed record CancelAppointmentCommand(Guid Id) : ICommand<Unit>;
public sealed record NoShowAppointmentCommand(Guid Id) : ICommand<Unit>;
```

- [ ] **Step 1: Create the records** exactly as the Interfaces block above (one file each; `using Mediator;` and `using FSH.Modules.Scheduling.Contracts.Dtos;` where needed). `Status` is the enum name as string.

- [ ] **Step 2: Build**

Run: `dotnet build src/Modules/Scheduling/Modules.Scheduling.Contracts/Modules.Scheduling.Contracts.csproj -clp:ErrorsOnly`
Expected: 0 errors.

- [ ] **Step 3: Commit**
```bash
git add -A && git commit -m "feat(scheduling): Appointment contracts (dto, commands, queries)"
```

---

## Task 6: Permissions resource + cross-module ref validator helper

**Files:**
- Create: `src/Modules/Scheduling/Modules.Scheduling.Contracts/Authorization/SchedulingPermissions.cs`
- Create: `src/Modules/Scheduling/Modules.Scheduling/Features/v1/Appointments/AppointmentRefValidator.cs` (shared existence checks via Mediator)
- Modify: `SchedulingModule.cs` (`PermissionConstants.Register(SchedulingPermissions.All)`)

**Interfaces:**
- Produces:
  - `SchedulingPermissions.Appointments` with `Resource = "Scheduling.Appointments"`, consts `View/Create/Update/Delete`, and `All` (mirror `AdministrationPermissions` shape; `View` `IsBasic: true`).
  - `AppointmentRefValidator.EnsureRefsExistAsync(IMediator mediator, Guid clinicId, Guid providerId, Guid? appointmentTypeId, Guid? patientId, CancellationToken ct)` — throws `NotFoundException` if any ref is missing, using `GetClinicByIdQuery`, `GetProviderByIdQuery`, `ListAppointmentTypesQuery` (membership check), `GetPatientByIdQuery`.

- [ ] **Step 1: Create `SchedulingPermissions.cs`** mirroring `AdministrationPermissions.cs` (just the `Appointments` nested class + an `All` list with the four `FshPermission` entries; `View` `IsBasic: true`).

- [ ] **Step 2: Create `AppointmentRefValidator.cs`**

```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Clinics;          // GetClinicByIdQuery
using FSH.Modules.Administration.Contracts.v1.Providers;        // GetProviderByIdQuery
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes; // ListAppointmentTypesQuery
using FSH.Modules.Patient.Contracts.v1.Patients;               // GetPatientByIdQuery
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.Appointments;

internal static class AppointmentRefValidator
{
    public static async ValueTask EnsureRefsExistAsync(
        IMediator mediator, Guid clinicId, Guid providerId, Guid? appointmentTypeId, Guid? patientId,
        CancellationToken ct)
    {
        // Clinic + Provider are required. These queries already throw NotFound when absent.
        _ = await mediator.Send(new GetClinicByIdQuery(clinicId), ct).ConfigureAwait(false);
        _ = await mediator.Send(new GetProviderByIdQuery(providerId), ct).ConfigureAwait(false);

        if (patientId is { } pid)
        {
            _ = await mediator.Send(new GetPatientByIdQuery(pid), ct).ConfigureAwait(false);
        }

        if (appointmentTypeId is { } typeId)
        {
            var types = await mediator.Send(new ListAppointmentTypesQuery(), ct).ConfigureAwait(false);
            if (types.All(t => t.Id != typeId))
            {
                throw new NotFoundException($"Appointment type {typeId} not found.");
            }
        }
    }
}
```
*Note:* confirm the exact contract type names/namespaces while implementing (e.g. `GetClinicByIdQuery`, `GetProviderByIdQuery`, `GetPatientByIdQuery`); adjust `using`s to match. If a `GetProviderByIdQuery` returns nullable instead of throwing, add an explicit null→`NotFoundException` guard.

- [ ] **Step 3: Register permissions** — in `SchedulingModule.ConfigureServices` add `PermissionConstants.Register(SchedulingPermissions.All);` (mirror Administration).

- [ ] **Step 4: Build + commit**

Run: `dotnet build src/FSH.Starter.slnx -clp:ErrorsOnly` → 0 errors.
```bash
git add -A && git commit -m "feat(scheduling): permissions + cross-module ref validator"
```

---

## Task 7: Feature slices — Create / Update / Delete / GetById / ListRange

**Files (under `src/Modules/Scheduling/Modules.Scheduling/Features/v1/Appointments/`):**
- Create per feature folder: `{Feature}/{Feature}CommandHandler.cs` (or `QueryHandler`), `{Feature}CommandValidator.cs` (commands + the range query), `{Feature}Endpoint.cs`.
- Modify: `SchedulingModule.cs` (`MapEndpoints` — register all endpoints).
- Test: extend `src/Tests/Scheduling.Tests` with validator tests.

**Interfaces:**
- Consumes: contracts (Task 5), `SchedulingDbContext.Appointments` (Task 4), `AppointmentRefValidator` (Task 6), `SchedulingPermissions` (Task 6).
- Produces: REST endpoints under `/api/v1/scheduling`: `GET/POST /appointments`, `GET/PUT/DELETE /appointments/{id:guid}`.

Use the Administration `AppointmentTypes` slice files as the literal template (same handler/validator/endpoint shape). Specifics:

- [ ] **Step 1: `ListAppointments` (range query) handler** — `Features/v1/Appointments/ListAppointments/ListAppointmentsQueryHandler.cs`:

```csharp
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListAppointments;

public sealed class ListAppointmentsQueryHandler(SchedulingDbContext dbContext)
    : IQueryHandler<ListAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    public async ValueTask<IReadOnlyList<AppointmentDto>> Handle(ListAppointmentsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<Domain.Appointment> q = dbContext.Appointments.AsNoTracking()
            .Where(a => a.ClinicId == query.ClinicId && a.StartUtc < query.ToUtc && a.EndUtc > query.FromUtc);

        if (query.ProviderIds is { Count: > 0 } ids)
        {
            q = q.Where(a => ids.Contains(a.ProviderId));
        }

        return await q.OrderBy(a => a.StartUtc)
            .Select(a => new AppointmentDto(a.Id, a.ClinicId, a.ProviderId, a.PatientId, a.AppointmentTypeId,
                a.StartUtc, a.EndUtc, a.Notes, a.Status.ToString(), a.Cancelled, a.NoShow))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
```

- [ ] **Step 2: `ListAppointments` validator** — required because it's a range query handler (Architecture.Tests). `Features/v1/Appointments/ListAppointments/ListAppointmentsQueryValidator.cs`:
```csharp
using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListAppointments;

public sealed class ListAppointmentsQueryValidator : AbstractValidator<ListAppointmentsQuery>
{
    public ListAppointmentsQueryValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.ToUtc).GreaterThan(x => x.FromUtc);
    }
}
```

- [ ] **Step 3: `ListAppointments` endpoint** — `GET /appointments?clinicId&providerIds&fromUtc&toUtc`:
```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListAppointments;

public static class ListAppointmentsEndpoint
{
    internal static RouteHandlerBuilder MapListAppointmentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/appointments",
                (Guid clinicId, DateTime fromUtc, DateTime toUtc, Guid[]? providerIds, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListAppointmentsQuery(clinicId, providerIds, fromUtc, toUtc), ct))
            .WithName("ListAppointments")
            .WithSummary("List appointments in a date range for a clinic")
            .RequirePermission(SchedulingPermissions.Appointments.View);
    }
}
```

- [ ] **Step 4: `CreateAppointment`** handler/validator/endpoint. Handler validates refs then persists + broadcasts is added in Task 8; for now create + save + return id:
```csharp
// CreateAppointmentCommandHandler.cs
public sealed class CreateAppointmentCommandHandler(SchedulingDbContext dbContext, IMediator mediator)
    : ICommandHandler<CreateAppointmentCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        await AppointmentRefValidator.EnsureRefsExistAsync(
            mediator, command.ClinicId, command.ProviderId, command.AppointmentTypeId, command.PatientId, cancellationToken)
            .ConfigureAwait(false);

        var entity = Domain.Appointment.Create(command.ClinicId, command.ProviderId, command.PatientId,
            command.AppointmentTypeId, command.StartUtc, command.EndUtc, command.Notes);
        await dbContext.Appointments.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
```
Validator: `ClinicId`/`ProviderId` `NotEmpty()`, `EndUtc` `GreaterThan(StartUtc)`, `Notes` `MaximumLength(4000)`. Endpoint: `POST /appointments` `.RequirePermission(SchedulingPermissions.Appointments.Create)`.

- [ ] **Step 5: `UpdateAppointment` / `DeleteAppointment` / `GetAppointmentById`** — clone the Administration `AppointmentTypes` Update/Delete/(+ a GetById mirroring `GetClinicById`) slices. Update calls `EnsureRefsExistAsync` then `entity.Update(...)`; Delete loads + `entity.Delete(null)`; GetById projects to `AppointmentDto` or throws `NotFoundException`. Update/Delete validators: `Id` `NotEmpty()` (+ Update mirrors Create rules).

- [ ] **Step 6: Register endpoints** in `SchedulingModule.MapEndpoints`:
```csharp
group.MapListAppointmentsEndpoint();
group.MapGetAppointmentByIdEndpoint();
group.MapCreateAppointmentEndpoint();
group.MapUpdateAppointmentEndpoint();
group.MapDeleteAppointmentEndpoint();
```

- [ ] **Step 7: Build + run unit tests + commit**

Run: `dotnet build src/FSH.Starter.slnx -clp:ErrorsOnly` → 0 errors. `dotnet test src/Tests/Scheduling.Tests/Scheduling.Tests.csproj` → PASS.
```bash
git add -A && git commit -m "feat(scheduling): appointment CRUD + range-query slices"
```

---

## Task 8: Lifecycle slices + SignalR `AppointmentChanged` broadcast

**Files:**
- Create: `Features/v1/Appointments/{CheckIn,CheckOut,Cancel,NoShow}/{...CommandHandler,Validator,Endpoint}.cs`
- Create: `Features/v1/Appointments/AppointmentRealtimeNotifier.cs` (wraps `IHubContext<AppHub>`)
- Modify: Create/Update/Delete/lifecycle handlers to call the notifier after `SaveChanges`.
- Modify: `SchedulingModule.cs` (`MapEndpoints` + DI registration of the notifier).

**Interfaces:**
- Consumes: `IHubContext<AppHub>` from `FSH.Framework.Web.Realtime` (read `.agents/rules/realtime.md` for the exact type/namespace + how other modules resolve the tenant id).
- Produces: lifecycle endpoints `POST /appointments/{id:guid}/{check-in|check-out|cancel|no-show}`; SignalR event `"AppointmentChanged"` to group `tenant:{tenantId}` payload `{ clinicId, providerId, startUtc, endUtc, action }`.

- [ ] **Step 1: Confirm the realtime primitives** — read `.agents/rules/realtime.md` and grep for an existing `IHubContext<AppHub>` user (e.g. Notifications' `"NotificationCreated"`) to copy the exact namespace, group-name helper, and how to read the current tenant id (likely `ICurrentTenant`/`IMultiTenantContextAccessor<AppTenantInfo>`).

- [ ] **Step 2: Create `AppointmentRealtimeNotifier.cs`**

```csharp
using FSH.Framework.Shared.Multitenancy;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Web.Realtime;            // confirm namespace of AppHub
using Microsoft.AspNetCore.SignalR;

namespace FSH.Modules.Scheduling.Features.v1.Appointments;

public sealed class AppointmentRealtimeNotifier(
    IHubContext<AppHub> hub,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
{
    public async Task NotifyChangedAsync(
        Guid clinicId, Guid providerId, DateTime startUtc, DateTime endUtc, string action, CancellationToken ct)
    {
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrEmpty(tenantId)) return;
        await hub.Clients.Group($"tenant:{tenantId}")
            .SendAsync("AppointmentChanged",
                new { clinicId, providerId, startUtc, endUtc, action }, ct)
            .ConfigureAwait(false);
    }
}
```
Register it in `SchedulingModule.ConfigureServices`: `builder.Services.AddScoped<AppointmentRealtimeNotifier>();`.

- [ ] **Step 3: Lifecycle slices** — for each of CheckIn/CheckOut/Cancel/NoShow, a handler that loads the appointment (`NotFoundException` if absent), calls the matching domain method, saves, then `await notifier.NotifyChangedAsync(entity.ClinicId, entity.ProviderId, entity.StartUtc, entity.EndUtc, "<action>", ct)`. Validators: `Id` `NotEmpty()`. Endpoints: `POST /appointments/{id:guid}/check-in` etc., `.RequirePermission(SchedulingPermissions.Appointments.Update)`.

- [ ] **Step 4: Wire the notifier into Create/Update/Delete** (Task 7 handlers) — inject `AppointmentRealtimeNotifier` and call `NotifyChangedAsync(..., "created"|"updated"|"deleted", ct)` after `SaveChangesAsync`.

- [ ] **Step 5: Register lifecycle endpoints** in `MapEndpoints` (`MapCheckInAppointmentEndpoint()`, etc.).

- [ ] **Step 6: Build + commit**

Run: `dotnet build src/FSH.Starter.slnx -clp:ErrorsOnly` → 0 errors.
```bash
git add -A && git commit -m "feat(scheduling): lifecycle slices + SignalR AppointmentChanged broadcast"
```

---

## Task 9: Integration tests (Testcontainers / Postgres)

**Files:**
- Create: `src/Tests/Integration.Tests/Tests/Scheduling/AppointmentsTests.cs` (mirror `Tests/Administration/*`)

**Interfaces:**
- Consumes: the running module via `FshWebApplicationFactory`, base path `/api/v1/scheduling`, root admin client.

- [ ] **Step 1: Write the integration tests** — read an existing `src/Tests/Integration.Tests/Tests/Administration/*.cs` for the harness pattern (how it seeds a clinic/provider/type, the authed client). Cover:
  1. `Create_Then_ListRange_Returns_Appointment` — seed clinic+provider (+ type), POST an appointment, GET range covering it → contains it; GET a non-overlapping range → empty.
  2. `CheckIn_Advances_Status` — POST create, POST `/check-in`, GET by id → `Status == "CheckedIn"`.
  3. `Create_With_Unknown_Clinic_Returns_404` — POST with random clinic id → 404 (ref validation).
  4. `TenantIsolation` — appointment created under tenant A not visible to tenant B (if the harness supports a second tenant, mirror the existing isolation test; else skip).

- [ ] **Step 2: Run (Docker required)**

Run: `dotnet test src/Tests/Integration.Tests/Integration.Tests.csproj --filter "FullyQualifiedName~Scheduling"`
Expected: PASS (Docker running).

- [ ] **Step 3: Commit**
```bash
git add -A && git commit -m "test(scheduling): appointment integration tests"
```

---

## Task 10: Apply migrations to the Aspire dev DB + smoke-test the API

**Files:** none (operational).

- [ ] **Step 1: Ensure the Aspire Postgres container is up** — `docker ps --filter name=postgres` (start it if exited: `docker start <name>`); note the host port.

- [ ] **Step 2: Apply migrations**

Run (override the connection string to the Aspire `fsh-db`, substituting the current port):
```bash
DatabaseOptions__ConnectionString="Host=localhost;Port=<port>;Database=fsh-db;Username=postgres;Password=<pw>" \
  dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply
```
Expected: log lines `[Administration] applied migrations` (Clinic.TimeZoneId) and `[Scheduling] applied migrations`.

- [ ] **Step 3: Verify the table**

Run: `docker exec -e PGPASSWORD=<pw> <container> psql -U postgres -d fsh-db -c "SELECT column_name, data_type FROM information_schema.columns WHERE table_schema='scheduling' AND table_name='Appointments' ORDER BY ordinal_position;"`
Expected: `StartUtc`/`EndUtc` = `timestamp with time zone`, `Status` = `text`, `TenantId` present.

- [ ] **Step 4: No commit** (operational only).

---

## Task 11: Dashboard — facade, nav, route

**Files:**
- Create: `clients/dashboard/src/api/scheduling.ts`
- Modify: `clients/dashboard/src/components/layout/nav-data.ts` (new top-level section "Schedule")
- Modify: `clients/dashboard/src/routes.tsx` (lazy route `/scheduler`)

**Interfaces:**
- Produces: `AppointmentDto` (TS), `listAppointments`, `getAppointment`, `createAppointment`, `updateAppointment`, `deleteAppointment`, `checkInAppointment`, `checkOutAppointment`, `cancelAppointment`, `noShowAppointment`.

- [ ] **Step 1: Create `scheduling.ts`** (mirror `administration.ts` `apiFetch` style):
```ts
import { apiFetch } from "@/lib/api-client";

export type AppointmentStatus = "Scheduled" | "CheckedIn" | "CheckedOut";

export type AppointmentDto = {
  id: string;
  clinicId: string;
  providerId: string;
  patientId?: string | null;
  appointmentTypeId?: string | null;
  startUtc: string;   // ISO …Z
  endUtc: string;     // ISO …Z
  notes?: string | null;
  status: AppointmentStatus;
  cancelled: boolean;
  noShow: boolean;
};

export type ListAppointmentsParams = {
  clinicId: string;
  providerIds?: string[];
  fromUtc: string;
  toUtc: string;
};

export function listAppointments(p: ListAppointmentsParams): Promise<AppointmentDto[]> {
  const q = new URLSearchParams({ clinicId: p.clinicId, fromUtc: p.fromUtc, toUtc: p.toUtc });
  for (const id of p.providerIds ?? []) q.append("providerIds", id);
  return apiFetch<AppointmentDto[]>(`/api/v1/scheduling/appointments?${q.toString()}`);
}

export type AppointmentInput = {
  clinicId: string; providerId: string; patientId?: string | null;
  appointmentTypeId?: string | null; startUtc: string; endUtc: string; notes?: string | null;
};

export function createAppointment(input: AppointmentInput): Promise<string> {
  return apiFetch<string>("/api/v1/scheduling/appointments", { method: "POST", body: JSON.stringify(input) });
}
export async function updateAppointment(id: string, input: AppointmentInput): Promise<void> {
  await apiFetch<void>(`/api/v1/scheduling/appointments/${encodeURIComponent(id)}`,
    { method: "PUT", body: JSON.stringify({ id, ...input }) });
}
export async function deleteAppointment(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/scheduling/appointments/${encodeURIComponent(id)}`, { method: "DELETE" });
}
const lifecycle = (verb: string) => async (id: string) => {
  await apiFetch<void>(`/api/v1/scheduling/appointments/${encodeURIComponent(id)}/${verb}`, { method: "POST" });
};
export const checkInAppointment = lifecycle("check-in");
export const checkOutAppointment = lifecycle("check-out");
export const cancelAppointment = lifecycle("cancel");
export const noShowAppointment = lifecycle("no-show");
```

- [ ] **Step 2: Nav section** — in `nav-data.ts` import `CalendarClock` (already imported) and add a new top-level section before/after Administration:
```ts
{
  id: "scheduling",
  caption: "Schedule",
  icon: CalendarClock,
  items: [
    { to: "/scheduler", label: "Scheduler", icon: CalendarClock, perm: "Permissions.Scheduling.Appointments.View" },
  ],
},
```

- [ ] **Step 3: Route** — in `routes.tsx` add the lazy import + route:
```tsx
const SchedulerPage = lazyNamed(() => import("@/pages/scheduler/scheduler"), "SchedulerPage");
// inside the AppShell children:
{ path: "scheduler", element: withSuspense(<SchedulerPage />) },
```

- [ ] **Step 4: tsc** — Run: `npx tsc --noEmit` (from `clients/dashboard`). Expected: passes (the page import resolves after Task 12; if running before Task 12, create a stub `scheduler.tsx` exporting `SchedulerPage` to keep tsc green, replaced in Task 12).

- [ ] **Step 5: Commit**
```bash
git add -A && git commit -m "feat(dashboard): scheduling facade + nav + route"
```

---

## Task 12: Dashboard — day-view calendar page

**Files:**
- Create: `clients/dashboard/src/pages/scheduler/scheduler.tsx` (page: filters + day grid)
- Create: `clients/dashboard/src/pages/scheduler/time-utils.ts` (UTC ↔ clinic-zone helpers)
- Modify: `clients/dashboard/package.json` if `date-fns-tz` is absent (check first: `grep date-fns clients/dashboard/package.json`).

**Interfaces:**
- Consumes: `listAppointments` + lookups (`listClinics`, `listProviders`, `listAppointmentTypes`, `getScheduleConfig` from `administration.ts`), `useRealtimeEvent` (Task 13 wires it; page exposes a `refetch`).
- Produces: `SchedulerPage` React component.

- [ ] **Step 1: Ensure a tz library** — check `clients/dashboard/package.json` for `date-fns-tz`. If missing: `cd clients/dashboard && npm install date-fns-tz` (date-fns is already used in the app; confirm with grep). Commit the lockfile change as part of this task.

- [ ] **Step 2: `time-utils.ts`** — clinic-zone conversions using `Intl` (no new dep needed if you prefer): given an appointment's `startUtc` (ISO) + clinic `timeZoneId`, compute the local wall-clock minutes-from-midnight for grid positioning, and the inverse (a clicked grid slot + selected date + zone → a UTC ISO start). Provide:
```ts
export function utcToZonedMinutes(iso: string, timeZoneId: string): number; // minutes from local midnight
export function zonedDateAndMinutesToUtcIso(dateYmd: string, minutes: number, timeZoneId: string): string;
export function formatZonedTime(iso: string, timeZoneId: string): string; // "h:mm a"
```
Implement with `date-fns-tz` `toZonedTime`/`fromZonedTime` (or `Intl.DateTimeFormat` with `timeZone`). Include 3 unit-style assertions in a comment block validating a known case (e.g. `2026-06-24T14:00:00Z` in `America/New_York` → 10:00 → 600 minutes).

- [ ] **Step 3: `scheduler.tsx`** — build the page (hand-rolled, Radix/Tailwind; follow `pages/administration/schedule.tsx` for the clinic/provider selects + card chrome):
  - **State:** `clinicId` (default first active clinic), `providerIds` (multi-select; default all active providers for the clinic), `date` (today). Resolve the clinic's `timeZoneId` from the clinics query.
  - **Schedule window:** fetch `getScheduleConfig(clinicId)` → `startTime`/`endTime`/`intervalMinutes` define the grid rows (clinic-local).
  - **Data:** `useQuery(["scheduling","appointments",{clinicId,providerIds,date}], () => listAppointments({ clinicId, providerIds, fromUtc: dayStartUtc, toUtc: dayEndUtc }))` where `dayStart/EndUtc` come from `zonedDateAndMinutesToUtcIso(date, 0/1440, tz)`.
  - **Grid:** provider columns (resource view); rows are time slots from `ScheduleConfig`. Position each appointment by `utcToZonedMinutes(startUtc, tz)` and height by duration. Color from its `AppointmentType` (look up in the types query); show patient name (resolve via Patient API — for Phase 1, fetch patient names for the visible `patientId`s with a small `useQuery` per page batch using `getPatient`/search; if no batch endpoint, call `getPatient` per id with `useQueries`). Reserved/cancelled styling deferred to later phases (cancelled shows struck-through).
  - **Interactions:** click empty slot → open booking dialog (Task — booking dialog can be a follow-up sub-task in this file or split; include a minimal create dialog here: patient search via Patient `searchPatients`, provider/type/time/duration/notes, Save → `createAppointment`); click an event → manage dialog with lifecycle buttons (`checkIn/checkOut/cancel/noShow`) + edit + delete. Each mutation invalidates the appointments query.
  - Render all times via `formatZonedTime(..., tz)`.

- [ ] **Step 4: tsc + eslint**

Run (from `clients/dashboard`): `npx tsc --noEmit` then `npx eslint src/pages/scheduler/scheduler.tsx src/pages/scheduler/time-utils.ts`
Expected: 0 errors (fix any `react-hooks/exhaustive-deps` by `useMemo`-stabilising derived arrays, as in `schedule.tsx`).

- [ ] **Step 5: Commit**
```bash
git add -A && git commit -m "feat(dashboard): scheduler day-view calendar (timezone-aware)"
```

---

## Task 13: Dashboard — realtime live updates (replace the poller)

**Files:**
- Modify: `clients/dashboard/src/pages/scheduler/scheduler.tsx`

**Interfaces:**
- Consumes: `useRealtimeEvent<T>(name)` from `clients/dashboard/src/realtime/realtime-context.tsx`.

- [ ] **Step 1: Read the realtime hook** — open `realtime-context.tsx`; confirm `useRealtimeEvent`'s signature (event name + handler) and that the provider is mounted app-wide.

- [ ] **Step 2: Subscribe in `scheduler.tsx`**

```tsx
import { useQueryClient } from "@tanstack/react-query";
import { useRealtimeEvent } from "@/realtime/realtime-context";
// ...
const queryClient = useQueryClient();
useRealtimeEvent<{ clinicId: string }>("AppointmentChanged", (payload) => {
  if (payload.clinicId === clinicId) {
    queryClient.invalidateQueries({ queryKey: ["scheduling", "appointments"] });
  }
});
```
This refetches the visible day on any tenant appointment change — no polling timer.

- [ ] **Step 3: tsc + eslint** — Run: `npx tsc --noEmit` + eslint on the file → 0 errors.

- [ ] **Step 4: Commit**
```bash
git add -A && git commit -m "feat(dashboard): live scheduler updates via SignalR AppointmentChanged"
```

---

## Task 14: Dashboard — Playwright route-mocked test

**Files:**
- Create: `clients/dashboard/tests/scheduler/scheduler.spec.ts` (mirror an existing test in `clients/dashboard/tests/**`)

- [ ] **Step 1: Read an existing spec** — e.g. `clients/dashboard/tests/tickets/tickets-list.spec.ts` + `clients/dashboard/tests/helpers/shell-mocks.ts` for the auth/permission + route-mock harness.

- [ ] **Step 2: Write the test** — grant `Permissions.Scheduling.Appointments.View` (+ Create), mock `GET /api/v1/scheduling/appointments` (one appointment), mock the clinics/providers/types/schedule-config lookups, navigate to `/scheduler`, assert the appointment renders in the provider column at the right time; then open the booking dialog, fill it, mock `POST .../appointments`, Save, assert the POST fired.

- [ ] **Step 3: Run**

Run (from `clients/dashboard`): `npx playwright test tests/scheduler/scheduler.spec.ts`
Expected: PASS.

- [ ] **Step 4: Commit**
```bash
git add -A && git commit -m "test(dashboard): scheduler Playwright route-mocked test"
```

---

## Self-Review (spec coverage)

- New `Scheduling` module + 4-place registration → **Task 1**. Own nav `/scheduler` → **Tasks 11–12**.
- `Clinic.TimeZoneId` (only edit outside the module) → **Task 2**.
- `Appointment` entity + status lifecycle → **Tasks 3, 8**. EF/migration → **Task 4**. Contracts → **Task 5**.
- Id-refs validated via Contracts (no cross-module FK), PHI stays in Patient → **Task 6** (`AppointmentRefValidator`), **Task 12** (patient name resolved from Patient API).
- CRUD + range query (+ validators) → **Task 7**. Permissions → **Task 6**.
- Multi-timezone (store UTC, render clinic-zone) → **Tasks 4, 12** (`time-utils.ts`).
- Realtime replacing the poller → **Task 8** (broadcast), **Task 13** (listener).
- Email reminders, reserved/recurring blocks, reschedule chains, week/month views → **deferred to Phase 2/3** (out of this plan, per spec phasing).
- Tests: unit **Tasks 2,3,7**, integration **Task 9**, dashboard tsc/eslint **Tasks 11–13**, Playwright **Task 14**.

## Verification (end-to-end, after Task 14)

- `dotnet build src/FSH.Starter.slnx` → 0 warn/0 err.
- `dotnet test src/FSH.Starter.slnx` → unit + Architecture green (integration needs Docker); only the 2 pre-existing Patient Architecture reds remain.
- DbMigrator `apply` → `scheduling.Appointments` + `Clinic.TimeZoneId` present (Task 10).
- Dashboard: `npm run dev`; book an appointment in tab A → it appears live in tab B (SignalR, no refresh). A Manila clinic and a New York clinic each render their own local day for the same UTC instants.
