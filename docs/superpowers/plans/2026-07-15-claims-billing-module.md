# Claims (Medical-Claims Billing) Module Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn saved super bills into trackable insurance claims — a new `Claims` bounded context that consumes `SuperBillSavedIntegrationEvent`, with a `Draft→Ready→Submitted→Paid/Denied/Voided` lifecycle, a stubbed submission seam, and a distinct dashboard **Billing** menu (worklist + claim detail).

**Architecture:** New `Modules.Claims` + `Modules.Claims.Contracts` projects (Vertical Slice Architecture, like every other module). Tenant-isolated `ClaimsDbContext : BaseDbContext` in its own `claims` schema. The only inbound coupling is an `IIntegrationEventHandler<SuperBillSavedIntegrationEvent>`; everything the claim needs is snapshotted from the event (no cross-module FKs, no cross-module reads). Display names are resolved client-side on the dashboard from data it already loads.

**Tech Stack:** .NET 10 · Mediator 3.x (`ICommand`/`IQuery` + source-gen) · FluentValidation 12 · EF Core 10 / PostgreSQL (Npgsql) · Finbuckle multitenancy · React 19 + Vite 7 + TanStack Query v5 + React Router 7 · Playwright (route-mocked) · xUnit + Shouldly + NSubstitute.

Design spec: `docs/superpowers/specs/2026-07-15-claims-billing-module-design.md`.

## Global Constraints

- **Module boundaries** — reference another module only through its `.Contracts` project. Enforced by `Architecture.Tests`. Claims references `Modules.Patient.Contracts` (for the event + `ReportProcedureItem`) only.
- **Registering a module touches FOUR places** — `src/Host/FSH.Starter.Api/Program.cs` Mediator `o.Assemblies` (Contracts marker + module type) **and** `moduleAssemblies` array, **and the identical pair in** `src/Host/FSH.Starter.DbMigrator/Program.cs`. A missing Mediator marker = handlers silently undiscovered.
- **Tenant isolation default-ON** via `BaseDbContext`; subclass calls `base.OnModelCreating(modelBuilder)` **LAST**.
- **Do NOT modify `src/BuildingBlocks`.** Do NOT modify the existing `Modules.Billing` (SaaS billing) — this is a separate context.
- **Mediator handlers must be `public sealed`**, return `ValueTask<T>`, and `.ConfigureAwait(false)` every await; propagate `CancellationToken`.
- **Every command handler + every paginated query handler needs a `{CommandOrQuery}Validator`** (FluentValidation). Enforced by `HandlerValidatorPairingTests`. New handlers are NOT whitelisted — no exceptions. `GetClaimById` is not paginated → no validator.
- **Structured logging only** — message templates / `[LoggerMessage]`; no string interpolation in log messages.
- **File-scoped namespaces · 4-space indent · `is null`/`is not null` · records for DTOs/events · `default!` for required non-nullable strings.** Build runs `TreatWarningsAsErrors` — warnings fail the build.
- **Snapshot, not FK** — `Claim`/`ClaimLine` store ids + code/description/charge copied from the event; never add an FK or a query into another module's schema.
- **Frontend: pass per-call data through `mutate(arg)`**, never via closed-over state.
- **Docs + changelog travel with the change** (golden rule #10) — see Task 13.
- **Module order:** use `850` in `[assembly: FshModule(typeof(ClaimsModule), 850)]` (orders need not be globally unique; 850 is free — the highest current is Scheduling 810).

---

## File Structure

**Backend — `src/Modules/Claims/Modules.Claims/`** (runtime)
- `ClaimsModule.cs` — `IModule`: DI, permission registration, endpoint mapping, health check.
- `Domain/ClaimStatus.cs` — status enum.
- `Domain/Claim.cs` — aggregate root + guarded transitions + snapshot upsert helpers.
- `Domain/ClaimLine.cs` — owned child (procedure snapshot).
- `Data/ClaimsDbContext.cs` — `BaseDbContext` subclass, `claims` schema.
- `Data/ClaimsDbInitializer.cs` — `IDbInitializer` (migrate only; no seed).
- `Data/Configurations/ClaimConfiguration.cs`, `Data/Configurations/ClaimLineConfiguration.cs`.
- `Features/v1/Claims/SuperBillSaved/SuperBillSavedIntegrationEventHandler.cs` — the seam consumer.
- `Features/v1/Claims/MarkReady/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Claims/Submit/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Claims/MarkPaid/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Claims/MarkDenied/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Claims/Void/{Handler,Validator,Endpoint}.cs`
- `Features/v1/Claims/GetClaims/{QueryHandler,Validator,Endpoint}.cs`
- `Features/v1/Claims/GetClaimById/{QueryHandler,Endpoint}.cs`
- `Features/v1/Claims/ClaimMappings.cs` — entity→DTO.
- `Submission/IClaimSubmitter.cs`, `Submission/StubClaimSubmitter.cs`.

**Backend — `src/Modules/Claims/Modules.Claims.Contracts/`** (public API)
- `ClaimsContractsMarker.cs`
- `Authorization/ClaimsPermissions.cs`
- `ClaimStatus.cs` — Contracts-side enum mirror (string-serialized over the wire).
- `Dtos/ClaimLineDto.cs`, `Dtos/ClaimListItemDto.cs`, `Dtos/ClaimDetailDto.cs`, `Dtos/ClaimsSummaryDto.cs`, `Dtos/ClaimsPageDto.cs`.
- `v1/Claims/MarkClaimReadyCommand.cs`, `SubmitClaimCommand.cs`, `MarkClaimPaidCommand.cs`, `MarkClaimDeniedCommand.cs`, `VoidClaimCommand.cs`.
- `v1/Claims/GetClaimsQuery.cs`, `GetClaimByIdQuery.cs`.

**Migrations** — `src/Host/FSH.Starter.Migrations.PostgreSQL/Claims/` (generated).

**Hosts** — edit `Api/Program.cs` + `DbMigrator/Program.cs` (four-place registration).

**Tests — `src/Tests/Claims.Tests/`** (new xUnit project)
- `Domain/ClaimTests.cs`
- `Features/SuperBillSavedIntegrationEventHandlerTests.cs`
- `Features/ClaimTransitionHandlerTests.cs`
- `Features/GetClaimsQueryHandlerTests.cs`
- `Submission/StubClaimSubmitterTests.cs`
- `Validators/ClaimValidatorsTests.cs`

**Frontend — `clients/dashboard/`**
- `src/api/claims.ts`
- `src/pages/billing/claims-list.tsx`
- `src/pages/billing/claim-detail.tsx`
- edit `src/routes.tsx`, `src/components/layout/nav-data.ts`
- `tests/billing/claims-worklist.spec.ts`

---

## Task 1: Scaffold Contracts project + permissions + marker

**Files:**
- Create: `src/Modules/Claims/Modules.Claims.Contracts/Modules.Claims.Contracts.csproj`
- Create: `src/Modules/Claims/Modules.Claims.Contracts/ClaimsContractsMarker.cs`
- Create: `src/Modules/Claims/Modules.Claims.Contracts/Authorization/ClaimsPermissions.cs`
- Create: `src/Modules/Claims/Modules.Claims.Contracts/ClaimStatus.cs`
- Modify: `src/FSH.Starter.slnx` (add both new projects)

**Interfaces:**
- Produces: `ClaimsPermissions.View` = `"Permissions.Claims.View"`, `ClaimsPermissions.Manage` = `"Permissions.Claims.Manage"`, `ClaimsPermissions.All`; marker `ClaimsContractsMarker`; enum `ClaimStatus { Draft, Ready, Submitted, Paid, Denied, Voided }`.

- [ ] **Step 1: Copy the Contracts csproj shape from Billing**

Open `src/Modules/Billing/Modules.Billing.Contracts/Modules.Billing.Contracts.csproj`, and create the Claims Contracts csproj with the same `<PropertyGroup>`/`<ItemGroup>` (same target framework, same package refs — typically `Mediator.Abstractions` + a ProjectReference to `FSH.Framework.Shared`). Add a `ProjectReference` to `src/Modules/Patient/Modules.Patient.Contracts/Modules.Patient.Contracts.csproj` (needed for `ReportProcedureItem` + the event in later tasks).

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Mirror Modules.Billing.Contracts.csproj exactly, then add: -->
  <ItemGroup>
    <ProjectReference Include="..\..\Patient\Modules.Patient.Contracts\Modules.Patient.Contracts.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write the marker**

```csharp
namespace FSH.Modules.Claims.Contracts;

/// <summary>Assembly marker for Mediator assembly scanning (Program.cs o.Assemblies).</summary>
public sealed class ClaimsContractsMarker;
```

- [ ] **Step 3: Write `ClaimStatus` (Contracts enum)**

```csharp
namespace FSH.Modules.Claims.Contracts;

public enum ClaimStatus
{
    Draft,
    Ready,
    Submitted,
    Paid,
    Denied,
    Voided,
}
```

- [ ] **Step 4: Write `ClaimsPermissions`** (mirror `BillingPermissions`)

```csharp
using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Claims.Contracts.Authorization;

public static class ClaimsPermissions
{
    public const string Resource = "Claims";
    public const string View   = $"Permissions.{Resource}.View";
    public const string Manage = $"Permissions.{Resource}.Manage";

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Claims",   ActionConstants.View, Resource, IsBasic: true),
        new("Manage Claims", "Manage",             Resource),
    ];
}
```

- [ ] **Step 5: Add both projects to the solution**

Run: `dotnet sln src/FSH.Starter.slnx add src/Modules/Claims/Modules.Claims.Contracts/Modules.Claims.Contracts.csproj`
(The runtime project is added in Task 3.)

- [ ] **Step 6: Build the Contracts project**

Run: `dotnet build src/Modules/Claims/Modules.Claims.Contracts/Modules.Claims.Contracts.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Claims/Modules.Claims.Contracts src/FSH.Starter.slnx
git commit -m "feat(claims): scaffold Contracts project (marker, permissions, status)"
```

---

## Task 2: Domain — `Claim` aggregate + `ClaimLine` + guarded transitions (TDD)

**Files:**
- Create: `src/Modules/Claims/Modules.Claims/Modules.Claims.csproj`
- Create: `src/Modules/Claims/Modules.Claims/Domain/ClaimStatus.cs`
- Create: `src/Modules/Claims/Modules.Claims/Domain/ClaimLine.cs`
- Create: `src/Modules/Claims/Modules.Claims/Domain/Claim.cs`
- Create: `src/Tests/Claims.Tests/Claims.Tests.csproj`
- Create: `src/Tests/Claims.Tests/Domain/ClaimTests.cs`

**Interfaces:**
- Produces:
  - `enum ClaimStatus { Draft, Ready, Submitted, Paid, Denied, Voided }` (runtime-side, in `FSH.Modules.Claims.Domain`).
  - `ClaimLine` — `record`-like snapshot with `Guid ProcedureCodeId, string Code, string? Description, decimal Charge, IReadOnlyList<Guid> DiagnosticIds`.
  - `Claim` (aggregate root): factory `Claim.CreateFromSuperBill(Guid superBillId, Guid reportId, Guid patientId, Guid? insuranceTypeId, bool isBilled, IEnumerable<ClaimLine> lines)`; `void RefreshSnapshot(Guid? insuranceTypeId, bool isBilled, IEnumerable<ClaimLine> lines)`; `bool IsSnapshotEditable => Status == ClaimStatus.Draft`; transitions `MarkReady()`, `Submit(string controlNumber)`, `MarkPaid()`, `MarkDenied()`, `Void()`; properties `Id, SuperBillId, ReportId, PatientId, InsuranceTypeId, Status, TotalCharge, ControlNumber, SubmittedAtUtc, ResolvedAtUtc, CreatedAtUtc, UpdatedAtUtc, IReadOnlyList<ClaimLine> Lines`.

- [ ] **Step 1: Create the runtime csproj**

Mirror `src/Modules/Billing/Modules.Billing/Modules.Billing.csproj`'s `<PropertyGroup>`/package refs, and add these ProjectReferences (matching Billing's set): the BuildingBlocks projects Billing references, plus `Modules.Claims.Contracts` and `Modules.Patient.Contracts`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <!-- Mirror Modules.Billing.csproj PropertyGroup + BuildingBlocks ProjectReferences, then add: -->
  <ItemGroup>
    <ProjectReference Include="..\Modules.Claims.Contracts\Modules.Claims.Contracts.csproj" />
    <ProjectReference Include="..\..\Patient\Modules.Patient.Contracts\Modules.Patient.Contracts.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create the test project + add to solution**

Mirror `src/Tests/Billing.Tests/Billing.Tests.csproj`; add a `ProjectReference` to `Modules.Claims`, `Modules.Claims.Contracts`, and `Modules.Patient.Contracts`.

Run:
```bash
dotnet sln src/FSH.Starter.slnx add src/Modules/Claims/Modules.Claims/Modules.Claims.csproj
dotnet sln src/FSH.Starter.slnx add src/Tests/Claims.Tests/Claims.Tests.csproj
```

- [ ] **Step 3: Write the runtime `ClaimStatus` enum**

```csharp
namespace FSH.Modules.Claims.Domain;

public enum ClaimStatus
{
    Draft,
    Ready,
    Submitted,
    Paid,
    Denied,
    Voided,
}
```

- [ ] **Step 4: Write `ClaimLine`**

```csharp
namespace FSH.Modules.Claims.Domain;

/// <summary>
/// A snapshot of one super-bill procedure line, copied from <c>ReportProcedureItem</c> at claim
/// creation/refresh time. Snapshot, not FK — the procedure/diagnostic ids are values, never joined.
/// </summary>
public sealed class ClaimLine
{
    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public Guid ProcedureCodeId { get; private set; }
    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Charge { get; private set; }
    public List<Guid> DiagnosticIds { get; private set; } = [];

    private ClaimLine() { }

    public static ClaimLine Create(
        Guid claimId, Guid procedureCodeId, string code, string? description, decimal charge,
        IEnumerable<Guid> diagnosticIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new ClaimLine
        {
            Id = Guid.CreateVersion7(),
            ClaimId = claimId,
            ProcedureCodeId = procedureCodeId,
            Code = code,
            Description = description,
            Charge = charge,
            DiagnosticIds = diagnosticIds?.ToList() ?? [],
        };
    }
}
```

- [ ] **Step 5: Write the failing domain tests**

```csharp
using FSH.Modules.Claims.Domain;
using Shouldly;
using Xunit;

namespace Claims.Tests.Domain;

public sealed class ClaimTests
{
    private static Claim NewDraft() => Claim.CreateFromSuperBill(
        superBillId: Guid.CreateVersion7(),
        reportId: Guid.CreateVersion7(),
        patientId: Guid.CreateVersion7(),
        insuranceTypeId: Guid.CreateVersion7(),
        isBilled: false,
        lines:
        [
            ClaimLine.Create(Guid.Empty, Guid.CreateVersion7(), "99213", "Office visit", 120m, []),
            ClaimLine.Create(Guid.Empty, Guid.CreateVersion7(), "93000", "EKG", 150m, [Guid.CreateVersion7()]),
        ]);

    [Fact]
    public void CreateFromSuperBill_Starts_Draft_And_Sums_Charges()
    {
        var claim = NewDraft();
        claim.Status.ShouldBe(ClaimStatus.Draft);
        claim.TotalCharge.ShouldBe(270m);
        claim.Lines.Count.ShouldBe(2);
        claim.IsSnapshotEditable.ShouldBeTrue();
    }

    [Fact]
    public void RefreshSnapshot_Replaces_Lines_And_Total_While_Draft()
    {
        var claim = NewDraft();
        claim.RefreshSnapshot(insuranceTypeId: null, isBilled: false,
            lines: [ClaimLine.Create(Guid.Empty, Guid.CreateVersion7(), "36415", "Draw", 15m, [])]);
        claim.Lines.Count.ShouldBe(1);
        claim.TotalCharge.ShouldBe(15m);
        claim.InsuranceTypeId.ShouldBeNull();
    }

    [Fact]
    public void MarkReady_Then_Submit_Sets_ControlNumber_And_Timestamp()
    {
        var claim = NewDraft();
        claim.MarkReady();
        claim.Status.ShouldBe(ClaimStatus.Ready);
        claim.IsSnapshotEditable.ShouldBeFalse();

        claim.Submit("CTRL-123");
        claim.Status.ShouldBe(ClaimStatus.Submitted);
        claim.ControlNumber.ShouldBe("CTRL-123");
        claim.SubmittedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void MarkPaid_From_Submitted_Sets_Resolved()
    {
        var claim = NewDraft();
        claim.MarkReady();
        claim.Submit("C");
        claim.MarkPaid();
        claim.Status.ShouldBe(ClaimStatus.Paid);
        claim.ResolvedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Submit_From_Draft_Throws()
    {
        var claim = NewDraft();
        Should.Throw<InvalidOperationException>(() => claim.Submit("C"));
    }

    [Fact]
    public void MarkPaid_From_Ready_Throws()
    {
        var claim = NewDraft();
        claim.MarkReady();
        Should.Throw<InvalidOperationException>(() => claim.MarkPaid());
    }

    [Fact]
    public void Void_From_Ready_Is_Allowed_But_Void_From_Paid_Throws()
    {
        var claim = NewDraft();
        claim.MarkReady();
        claim.Void();
        claim.Status.ShouldBe(ClaimStatus.Voided);

        var paid = NewDraft();
        paid.MarkReady();
        paid.Submit("C");
        paid.MarkPaid();
        Should.Throw<InvalidOperationException>(() => paid.Void());
    }
}
```

- [ ] **Step 6: Run tests — verify they fail to compile (Claim not defined)**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj`
Expected: FAIL — `Claim` does not exist.

- [ ] **Step 7: Write `Claim`**

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Claims.Domain;

/// <summary>
/// An insurance claim originating from a saved super bill. Snapshot-only: every field is copied
/// from <c>SuperBillSavedIntegrationEvent</c> — no FK/joins into Patient/Administration. Lifecycle:
/// Draft → Ready → Submitted → Paid | Denied, plus Voided from any non-terminal state. The snapshot
/// is refreshable only while Draft (see <see cref="IsSnapshotEditable"/>); once a biller advances
/// the claim, a later super-bill re-save must not clobber it.
/// </summary>
public sealed class Claim : AggregateRoot<Guid>
{
    private readonly List<ClaimLine> _lines = new();

    public Guid SuperBillId { get; private set; }
    public Guid ReportId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid? InsuranceTypeId { get; private set; }
    public bool IsBilled { get; private set; }
    public ClaimStatus Status { get; private set; }
    public decimal TotalCharge { get; private set; }
    public string? ControlNumber { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? ResolvedAtUtc { get; private set; }

    public IReadOnlyList<ClaimLine> Lines => _lines;

    /// <summary>The snapshot may be refreshed from a re-saved super bill only while Draft.</summary>
    public bool IsSnapshotEditable => Status == ClaimStatus.Draft;

    private Claim() { }

    public static Claim CreateFromSuperBill(
        Guid superBillId, Guid reportId, Guid patientId, Guid? insuranceTypeId, bool isBilled,
        IEnumerable<ClaimLine> lines)
    {
        if (superBillId == Guid.Empty)
        {
            throw new ArgumentException("SuperBillId is required.", nameof(superBillId));
        }

        var claim = new Claim
        {
            Id = Guid.CreateVersion7(),
            SuperBillId = superBillId,
            ReportId = reportId,
            PatientId = patientId,
            InsuranceTypeId = insuranceTypeId,
            IsBilled = isBilled,
            Status = ClaimStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
        };
        claim.ReplaceLines(lines);
        return claim;
    }

    /// <summary>Refresh the snapshot from a re-saved super bill. No-op guard belongs to the caller
    /// (the event handler) which checks <see cref="IsSnapshotEditable"/> first.</summary>
    public void RefreshSnapshot(Guid? insuranceTypeId, bool isBilled, IEnumerable<ClaimLine> lines)
    {
        InsuranceTypeId = insuranceTypeId;
        IsBilled = isBilled;
        ReplaceLines(lines);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkReady()
    {
        RequireStatus(ClaimStatus.Draft, "mark ready");
        Status = ClaimStatus.Ready;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Submit(string controlNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(controlNumber);
        RequireStatus(ClaimStatus.Ready, "submit");
        Status = ClaimStatus.Submitted;
        ControlNumber = controlNumber;
        SubmittedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = SubmittedAtUtc;
    }

    public void MarkPaid()
    {
        RequireStatus(ClaimStatus.Submitted, "mark paid");
        Status = ClaimStatus.Paid;
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ResolvedAtUtc;
    }

    public void MarkDenied()
    {
        RequireStatus(ClaimStatus.Submitted, "mark denied");
        Status = ClaimStatus.Denied;
        ResolvedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = ResolvedAtUtc;
    }

    public void Void()
    {
        if (Status is ClaimStatus.Voided)
        {
            return; // idempotent
        }
        if (Status is ClaimStatus.Paid or ClaimStatus.Denied)
        {
            throw new InvalidOperationException($"Cannot void a claim in status {Status}.");
        }
        Status = ClaimStatus.Voided;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ReplaceLines(IEnumerable<ClaimLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        _lines.Clear();
        foreach (var line in lines)
        {
            _lines.Add(ClaimLine.Create(Id, line.ProcedureCodeId, line.Code, line.Description, line.Charge, line.DiagnosticIds));
        }
        TotalCharge = _lines.Sum(l => l.Charge);
    }

    private void RequireStatus(ClaimStatus expected, string action)
    {
        if (Status != expected)
        {
            throw new InvalidOperationException($"Cannot {action}: claim status is {Status}, expected {expected}.");
        }
    }
}
```

- [ ] **Step 8: Run tests — verify green**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj`
Expected: PASS (all `ClaimTests`).

- [ ] **Step 9: Commit**

```bash
git add src/Modules/Claims/Modules.Claims src/Tests/Claims.Tests src/FSH.Starter.slnx
git commit -m "feat(claims): Claim aggregate with guarded lifecycle transitions"
```

---

## Task 3: Data layer + module registration + migration

**Files:**
- Create: `src/Modules/Claims/Modules.Claims/Data/ClaimsDbContext.cs`
- Create: `src/Modules/Claims/Modules.Claims/Data/Configurations/ClaimConfiguration.cs`
- Create: `src/Modules/Claims/Modules.Claims/Data/Configurations/ClaimLineConfiguration.cs`
- Create: `src/Modules/Claims/Modules.Claims/Data/ClaimsDbInitializer.cs`
- Create: `src/Modules/Claims/Modules.Claims/ClaimsModule.cs`
- Modify: `src/Host/FSH.Starter.Api/Program.cs`
- Modify: `src/Host/FSH.Starter.DbMigrator/Program.cs`
- Modify: `src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj` and `src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj` (ProjectReference to `Modules.Claims`)
- Generated: `src/Host/FSH.Starter.Migrations.PostgreSQL/Claims/*`

**Interfaces:**
- Consumes: `Claim`, `ClaimLine`, `ClaimsPermissions` (Tasks 1–2).
- Produces: `ClaimsDbContext` (`DbSet<Claim> Claims`, `DbSet<ClaimLine> ClaimLines`, `const string Schema = "claims"`); `ClaimsModule : IModule`; `ClaimsDbInitializer : IDbInitializer`. `ClaimsModule.MapEndpoints` maps a group at `api/v{version:apiVersion}/claims` — later tasks add endpoints to it via a shared `RouteGroupBuilder`.

- [ ] **Step 1: Write `ClaimsDbContext`** (mirror `TicketsDbContext` — tenant-isolated, no PHI)

```csharp
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Claims.Data;

public sealed class ClaimsDbContext : BaseDbContext
{
    public const string Schema = "claims";

    public ClaimsDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<ClaimsDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<Claim> Claims => Set<Claim>();
    public DbSet<ClaimLine> ClaimLines => Set<ClaimLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClaimsDbContext).Assembly);
        // base LAST so BaseDbContext auto-apply sees fully-configured entities (incl. HasMany child).
        base.OnModelCreating(modelBuilder);
    }
}
```

- [ ] **Step 2: Write `ClaimConfiguration`**

```csharp
using FSH.Modules.Claims.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Claims.Data.Configurations;

public sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Claims");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SuperBillId).IsRequired();
        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.InsuranceTypeId);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.TotalCharge).HasPrecision(18, 4);
        builder.Property(x => x.ControlNumber).HasMaxLength(64);

        // One claim per super bill — idempotency key for the event handler upsert.
        builder.HasIndex(x => x.SuperBillId).IsUnique().HasDatabaseName("ux_claims_superbill");
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(l => l.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Claim.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(x => x.DomainEvents);
    }
}
```

- [ ] **Step 3: Write `ClaimLineConfiguration`** (DiagnosticIds as a primitive collection → `uuid[]`)

```csharp
using FSH.Modules.Claims.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Claims.Data.Configurations;

public sealed class ClaimLineConfiguration : IEntityTypeConfiguration<ClaimLine>
{
    public void Configure(EntityTypeBuilder<ClaimLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ClaimLines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClaimId).IsRequired();
        builder.Property(x => x.ProcedureCodeId).IsRequired();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.Charge).HasPrecision(18, 4);
        // EF Core 10 primitive collection → Npgsql maps List<Guid> to a uuid[] column.
        builder.PrimitiveCollection(x => x.DiagnosticIds);

        builder.HasIndex(x => x.ClaimId);
        builder.Ignore(x => x.DomainEvents);
    }
}
```

- [ ] **Step 4: Write `ClaimsDbInitializer`** (migrate only; no seed data)

```csharp
using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Claims.Data;

public sealed class ClaimsDbInitializer(
    ClaimsDbContext dbContext,
    ILogger<ClaimsDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Claims] applied migrations");
        }
    }

    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- [ ] **Step 5: Write `ClaimsModule`** (DI + permission registration + empty endpoint group + health check)

```csharp
using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Web.Modules;
using FSH.Modules.Claims.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Claims.ClaimsModule), 850)]

namespace FSH.Modules.Claims;

public sealed class ClaimsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FSH.Framework.Shared.Constants.PermissionConstants.Register(
            FSH.Modules.Claims.Contracts.Authorization.ClaimsPermissions.All);

        builder.Services.AddHeroDbContext<ClaimsDbContext>();
        builder.Services.AddScoped<IDbInitializer, ClaimsDbInitializer>();

        // Consume SuperBillSavedIntegrationEvent (Patient.Contracts) + register the stub submitter.
        builder.Services.AddIntegrationEventHandlers(typeof(ClaimsModule).Assembly);
        // Submission.IClaimSubmitter registration is added in Task 6.

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ClaimsDbContext>(name: "db:claims", failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { /* none */ }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/claims")
            .WithTags("Claims")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        // Endpoint mappings are added in Tasks 7–8:
        //   group.MapGetClaimsEndpoint(); group.MapGetClaimByIdEndpoint();
        //   group.MapMarkClaimReadyEndpoint(); group.MapSubmitClaimEndpoint();
        //   group.MapMarkClaimPaidEndpoint(); group.MapMarkClaimDeniedEndpoint(); group.MapVoidClaimEndpoint();
        _ = group;
    }
}
```

> Note: if the `AddIntegrationEventHandlers` overload requires at least one handler type in the assembly to compile/scan cleanly, it is satisfied once Task 5 lands. It is safe to register now (no handler = no-op scan).

- [ ] **Step 6: Add ProjectReferences from both hosts to `Modules.Claims`**

Run:
```bash
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj reference src/Modules/Claims/Modules.Claims/Modules.Claims.csproj
dotnet add src/Host/FSH.Starter.DbMigrator/FSH.Starter.DbMigrator.csproj reference src/Modules/Claims/Modules.Claims/Modules.Claims.csproj
```

- [ ] **Step 7: Register in `Api/Program.cs` (Mediator markers + moduleAssemblies)**

In `src/Host/FSH.Starter.Api/Program.cs`, inside `o.Assemblies = [ ... ]`, add next to the Billing pair:
```csharp
        typeof(FSH.Modules.Claims.Contracts.ClaimsContractsMarker),
        typeof(FSH.Modules.Claims.ClaimsModule),
```
And in the `moduleAssemblies` array add:
```csharp
    typeof(FSH.Modules.Claims.ClaimsModule).Assembly,
```

- [ ] **Step 8: Register the identical pair in `DbMigrator/Program.cs`**

Apply the **same two edits** (Mediator `o.Assemblies` markers + `moduleAssemblies` array) in `src/Host/FSH.Starter.DbMigrator/Program.cs`. Verify by grepping:

Run: `grep -n "Claims" src/Host/FSH.Starter.Api/Program.cs src/Host/FSH.Starter.DbMigrator/Program.cs`
Expected: 3 lines in each file (Contracts marker, module type, moduleAssemblies entry).

- [ ] **Step 9: Build (snapshot must be current before generating a migration)**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 10: Generate the initial migration**

Run:
```bash
dotnet ef migrations add InitialClaims \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context ClaimsDbContext \
  --output-dir Claims
```
Expected: creates `src/Host/FSH.Starter.Migrations.PostgreSQL/Claims/<timestamp>_InitialClaims.cs` (+ `.Designer.cs`) and `ClaimsDbContextModelSnapshot.cs`. Confirm the migration creates schema `claims`, tables `Claims` + `ClaimLines`, the unique index `ux_claims_superbill`, and a `uuid[]` column for `DiagnosticIds`.

- [ ] **Step 11: Apply the migration to a dev DB**

Run: `dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply`
Expected: log line `[Claims] applied migrations` and no errors. (Requires the dev Postgres running.)

- [ ] **Step 12: Commit**

```bash
git add src/Modules/Claims src/Host/FSH.Starter.Api src/Host/FSH.Starter.DbMigrator src/Host/FSH.Starter.Migrations.PostgreSQL/Claims
git commit -m "feat(claims): data layer, module registration (4 places), initial migration"
```

---

## Task 4: Contracts — DTOs, commands, queries, submitter interface

**Files:**
- Create: `src/Modules/Claims/Modules.Claims.Contracts/Dtos/ClaimLineDto.cs`
- Create: `.../Dtos/ClaimListItemDto.cs`
- Create: `.../Dtos/ClaimDetailDto.cs`
- Create: `.../Dtos/ClaimsSummaryDto.cs`
- Create: `.../Dtos/ClaimsPageDto.cs`
- Create: `.../v1/Claims/MarkClaimReadyCommand.cs`
- Create: `.../v1/Claims/SubmitClaimCommand.cs`
- Create: `.../v1/Claims/MarkClaimPaidCommand.cs`
- Create: `.../v1/Claims/MarkClaimDeniedCommand.cs`
- Create: `.../v1/Claims/VoidClaimCommand.cs`
- Create: `.../v1/Claims/GetClaimsQuery.cs`
- Create: `.../v1/Claims/GetClaimByIdQuery.cs`
- Create: `src/Modules/Claims/Modules.Claims/Submission/IClaimSubmitter.cs`

**Interfaces:**
- Produces (all in `FSH.Modules.Claims.Contracts.*`):
  - `record ClaimLineDto(Guid ProcedureCodeId, string Code, string? Description, decimal Charge, IReadOnlyList<Guid> DiagnosticIds)`
  - `record ClaimListItemDto(Guid Id, Guid SuperBillId, Guid ReportId, Guid PatientId, Guid? InsuranceTypeId, ClaimStatus Status, decimal TotalCharge, int LineCount, DateTime CreatedAtUtc, DateTime? SubmittedAtUtc, DateTime? ResolvedAtUtc)`
  - `record ClaimDetailDto(Guid Id, Guid SuperBillId, Guid ReportId, Guid PatientId, Guid? InsuranceTypeId, ClaimStatus Status, decimal TotalCharge, string? ControlNumber, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc, DateTime? SubmittedAtUtc, DateTime? ResolvedAtUtc, IReadOnlyList<ClaimLineDto> Lines)`
  - `record ClaimsSummaryDto(int Draft, int Ready, int Submitted, int Paid, int Denied, decimal OutstandingCharge)` (Outstanding = Σ TotalCharge where Status ∈ {Ready, Submitted})
  - `record ClaimsPageDto(PagedResponse<ClaimListItemDto> Page, ClaimsSummaryDto Summary)`
  - `record MarkClaimReadyCommand(Guid ClaimId) : ICommand<Guid>` (and the four analogous commands; `VoidClaimCommand(Guid ClaimId, string? Reason = null)`)
  - `record GetClaimsQuery(ClaimStatus? Status = null, Guid? InsuranceTypeId = null, string? Search = null, int PageNumber = 1, int PageSize = 20) : IQuery<ClaimsPageDto>`
  - `record GetClaimByIdQuery(Guid ClaimId) : IQuery<ClaimDetailDto>`
  - `interface IClaimSubmitter { ValueTask<string> SubmitAsync(ClaimDetailDto claim, CancellationToken ct = default); }` (returns the control number)

- [ ] **Step 1: Write the DTOs**

```csharp
// Dtos/ClaimLineDto.cs
namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimLineDto(
    Guid ProcedureCodeId,
    string Code,
    string? Description,
    decimal Charge,
    IReadOnlyList<Guid> DiagnosticIds);
```

```csharp
// Dtos/ClaimListItemDto.cs
namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimListItemDto(
    Guid Id,
    Guid SuperBillId,
    Guid ReportId,
    Guid PatientId,
    Guid? InsuranceTypeId,
    ClaimStatus Status,
    decimal TotalCharge,
    int LineCount,
    DateTime CreatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? ResolvedAtUtc);
```

```csharp
// Dtos/ClaimDetailDto.cs
namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimDetailDto(
    Guid Id,
    Guid SuperBillId,
    Guid ReportId,
    Guid PatientId,
    Guid? InsuranceTypeId,
    ClaimStatus Status,
    decimal TotalCharge,
    string? ControlNumber,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? ResolvedAtUtc,
    IReadOnlyList<ClaimLineDto> Lines);
```

```csharp
// Dtos/ClaimsSummaryDto.cs
namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimsSummaryDto(
    int Draft,
    int Ready,
    int Submitted,
    int Paid,
    int Denied,
    decimal OutstandingCharge);
```

```csharp
// Dtos/ClaimsPageDto.cs
using FSH.Framework.Shared.Persistence;

namespace FSH.Modules.Claims.Contracts.Dtos;

public sealed record ClaimsPageDto(
    PagedResponse<ClaimListItemDto> Page,
    ClaimsSummaryDto Summary);
```

- [ ] **Step 2: Write the five transition commands**

```csharp
// v1/Claims/MarkClaimReadyCommand.cs
using Mediator;
namespace FSH.Modules.Claims.Contracts.v1.Claims;
public sealed record MarkClaimReadyCommand(Guid ClaimId) : ICommand<Guid>;
```
```csharp
// v1/Claims/SubmitClaimCommand.cs
using Mediator;
namespace FSH.Modules.Claims.Contracts.v1.Claims;
public sealed record SubmitClaimCommand(Guid ClaimId) : ICommand<Guid>;
```
```csharp
// v1/Claims/MarkClaimPaidCommand.cs
using Mediator;
namespace FSH.Modules.Claims.Contracts.v1.Claims;
public sealed record MarkClaimPaidCommand(Guid ClaimId) : ICommand<Guid>;
```
```csharp
// v1/Claims/MarkClaimDeniedCommand.cs
using Mediator;
namespace FSH.Modules.Claims.Contracts.v1.Claims;
public sealed record MarkClaimDeniedCommand(Guid ClaimId) : ICommand<Guid>;
```
```csharp
// v1/Claims/VoidClaimCommand.cs
using Mediator;
namespace FSH.Modules.Claims.Contracts.v1.Claims;
public sealed record VoidClaimCommand(Guid ClaimId, string? Reason = null) : ICommand<Guid>;
```

- [ ] **Step 3: Write the two queries**

```csharp
// v1/Claims/GetClaimsQuery.cs
using FSH.Modules.Claims.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record GetClaimsQuery(
    ClaimStatus? Status = null,
    Guid? InsuranceTypeId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<ClaimsPageDto>;
```
```csharp
// v1/Claims/GetClaimByIdQuery.cs
using FSH.Modules.Claims.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record GetClaimByIdQuery(Guid ClaimId) : IQuery<ClaimDetailDto>;
```

- [ ] **Step 4: Write the submitter interface** (runtime project, not Contracts — it consumes a Contracts DTO but is an internal seam)

```csharp
using FSH.Modules.Claims.Contracts.Dtos;

namespace FSH.Modules.Claims.Submission;

/// <summary>
/// Outbound seam to a clearinghouse. This slice ships <c>StubClaimSubmitter</c>; real
/// Cvikota/Kareo/OfficeAlly submitters land later behind this same interface.
/// </summary>
public interface IClaimSubmitter
{
    /// <summary>Submits the claim and returns the payer/clearinghouse control number.</summary>
    ValueTask<string> SubmitAsync(ClaimDetailDto claim, CancellationToken ct = default);
}
```

- [ ] **Step 5: Build**

Run: `dotnet build src/Modules/Claims/Modules.Claims/Modules.Claims.csproj`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Claims
git commit -m "feat(claims): contracts DTOs, commands, queries, submitter seam"
```

---

## Task 5: SuperBillSaved event handler — upsert + freeze-past-Draft (TDD)

**Files:**
- Create: `src/Modules/Claims/Modules.Claims/Features/v1/Claims/SuperBillSaved/SuperBillSavedIntegrationEventHandler.cs`
- Create: `src/Tests/Claims.Tests/Features/SuperBillSavedIntegrationEventHandlerTests.cs`

**Interfaces:**
- Consumes: `SuperBillSavedIntegrationEvent` + `ReportProcedureItem` (`FSH.Modules.Patient.Contracts.*`), `ClaimsDbContext`, `Claim`, `ClaimLine`.
- Produces: `SuperBillSavedIntegrationEventHandler : IIntegrationEventHandler<SuperBillSavedIntegrationEvent>`.

- [ ] **Step 1: Write the failing handler tests** (in-memory `ClaimsDbContext`, pattern from `CreatePatientCommandHandlerTests`)

```csharp
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Claims.Features.v1.Claims.SuperBillSaved;
using FSH.Modules.Patient.Contracts.Events;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Claims.Tests.Features;

public sealed class SuperBillSavedIntegrationEventHandlerTests
{
    private static ClaimsDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(
            new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));
        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql", ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });
        return new ClaimsDbContext(accessor, options, settings, Substitute.For<IHostEnvironment>());
    }

    private static SuperBillSavedIntegrationEvent Event(
        Guid superBillId, Guid? insuranceTypeId, params ReportProcedureItem[] procedures) =>
        new(
            Id: Guid.NewGuid(), OccurredOnUtc: DateTime.UtcNow, TenantId: "test",
            CorrelationId: Guid.NewGuid().ToString(), Source: "Patient",
            SuperBillId: superBillId, ReportId: Guid.NewGuid(), PatientId: Guid.NewGuid(),
            IsBilled: false, Procedures: procedures, InsuranceTypeId: insuranceTypeId);

    private static ReportProcedureItem Proc(string code, decimal charge) =>
        new(ProcedureCodeId: Guid.NewGuid(), Code: code, Description: code, Charge: charge, DiagnosticIds: []);

    [Fact]
    public async Task Creates_Draft_Claim_Snapshotting_Lines_And_Total()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
        var sb = Guid.NewGuid();

        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("99213", 120m), Proc("93000", 150m)), CancellationToken.None);

        var claim = await db.Claims.Include(c => c.Lines).SingleAsync();
        claim.SuperBillId.ShouldBe(sb);
        claim.Status.ShouldBe(ClaimStatus.Draft);
        claim.TotalCharge.ShouldBe(270m);
        claim.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Resave_While_Draft_Refreshes_Snapshot_In_Place()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
        var sb = Guid.NewGuid();
        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("99213", 120m)), CancellationToken.None);

        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("36415", 15m)), CancellationToken.None);

        var claim = await db.Claims.Include(c => c.Lines).SingleAsync();
        claim.Lines.Count.ShouldBe(1);
        claim.TotalCharge.ShouldBe(15m);
    }

    [Fact]
    public async Task Resave_Past_Draft_Is_Skipped_Freeze()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
        var sb = Guid.NewGuid();
        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("99213", 120m)), CancellationToken.None);
        var claim = await db.Claims.SingleAsync();
        claim.MarkReady();
        await db.SaveChangesAsync();

        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("36415", 15m)), CancellationToken.None);

        var after = await db.Claims.Include(c => c.Lines).SingleAsync();
        after.Status.ShouldBe(ClaimStatus.Ready);
        after.TotalCharge.ShouldBe(120m); // unchanged
        after.Lines.Single().Code.ShouldBe("99213");
    }

    [Fact]
    public async Task Redelivery_Does_Not_Create_Duplicate()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
        var sb = Guid.NewGuid();
        var evt = Event(sb, Guid.NewGuid(), Proc("99213", 120m));

        await sut.HandleAsync(evt, CancellationToken.None);
        await sut.HandleAsync(evt, CancellationToken.None);

        (await db.Claims.CountAsync()).ShouldBe(1);
    }
}
```

- [ ] **Step 2: Run — verify fail (handler type missing)**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter SuperBillSavedIntegrationEventHandlerTests`
Expected: FAIL — type not found.

- [ ] **Step 3: Write the handler**

```csharp
using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Patient.Contracts.Events;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Claims.Features.v1.Claims.SuperBillSaved;

/// <summary>
/// Upserts a claim from a saved super bill, keyed by SuperBillId. New → create Draft. Existing &amp;
/// still Draft → refresh the snapshot. Existing &amp; past Draft → skip (freeze so a biller's work
/// isn't clobbered by a late chart edit). Idempotent on redelivery.
/// </summary>
public sealed class SuperBillSavedIntegrationEventHandler(
    ClaimsDbContext db,
    ILogger<SuperBillSavedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<SuperBillSavedIntegrationEvent>
{
    public async Task HandleAsync(SuperBillSavedIntegrationEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var existing = await db.Claims
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.SuperBillId == @event.SuperBillId, ct)
            .ConfigureAwait(false);

        var lines = ToLines(@event.Procedures);

        if (existing is null)
        {
            var claim = Claim.CreateFromSuperBill(
                @event.SuperBillId, @event.ReportId, @event.PatientId,
                @event.InsuranceTypeId, @event.IsBilled, lines);
            db.Claims.Add(claim);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("[Claims] created draft claim {ClaimId} from super bill {SuperBillId}",
                claim.Id, @event.SuperBillId);
            return;
        }

        if (!existing.IsSnapshotEditable)
        {
            logger.LogInformation(
                "[Claims] super bill {SuperBillId} re-saved but claim {ClaimId} is {Status}; snapshot frozen",
                @event.SuperBillId, existing.Id, existing.Status);
            return;
        }

        existing.RefreshSnapshot(@event.InsuranceTypeId, @event.IsBilled, lines);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("[Claims] refreshed draft claim {ClaimId} from super bill {SuperBillId}",
            existing.Id, @event.SuperBillId);
    }

    private static IReadOnlyList<ClaimLine> ToLines(IReadOnlyList<ReportProcedureItem> procedures) =>
        (procedures ?? [])
            .Select(p => ClaimLine.Create(Guid.Empty, p.ProcedureCodeId, p.Code, p.Description, p.Charge, p.DiagnosticIds))
            .ToList();
}
```

- [ ] **Step 4: Run — verify green**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter SuperBillSavedIntegrationEventHandlerTests`
Expected: PASS (all four).

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Claims src/Tests/Claims.Tests
git commit -m "feat(claims): consume SuperBillSaved -> upsert claim with freeze-past-draft"
```

---

## Task 6: Stub submitter (TDD) + DI registration

**Files:**
- Create: `src/Modules/Claims/Modules.Claims/Submission/StubClaimSubmitter.cs`
- Create: `src/Tests/Claims.Tests/Submission/StubClaimSubmitterTests.cs`
- Modify: `src/Modules/Claims/Modules.Claims/ClaimsModule.cs` (register `IClaimSubmitter`)

**Interfaces:**
- Consumes: `IClaimSubmitter`, `ClaimDetailDto`.
- Produces: `StubClaimSubmitter : IClaimSubmitter` — returns a deterministic control number `"STUB-" + short(claim.Id)`.

- [ ] **Step 1: Write the failing test**

```csharp
using FSH.Modules.Claims.Contracts;
using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Submission;
using Shouldly;
using Xunit;

namespace Claims.Tests.Submission;

public sealed class StubClaimSubmitterTests
{
    [Fact]
    public async Task SubmitAsync_Returns_NonEmpty_Control_Number()
    {
        var claim = new ClaimDetailDto(
            Id: Guid.NewGuid(), SuperBillId: Guid.NewGuid(), ReportId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(), InsuranceTypeId: null, Status: ClaimStatus.Ready,
            TotalCharge: 100m, ControlNumber: null, CreatedAtUtc: DateTime.UtcNow, UpdatedAtUtc: null,
            SubmittedAtUtc: null, ResolvedAtUtc: null, Lines: []);

        var sut = new StubClaimSubmitter();
        var control = await sut.SubmitAsync(claim, CancellationToken.None);

        control.ShouldNotBeNullOrWhiteSpace();
        control.ShouldStartWith("STUB-");
    }
}
```

- [ ] **Step 2: Run — verify fail**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter StubClaimSubmitterTests`
Expected: FAIL — type not found.

- [ ] **Step 3: Write `StubClaimSubmitter`**

```csharp
using FSH.Modules.Claims.Contracts.Dtos;

namespace FSH.Modules.Claims.Submission;

/// <summary>
/// Placeholder submitter for this slice: does not transmit anywhere; returns a synthetic control
/// number so the worklist can show a "Submitted" claim. Replace per-tenant with a real clearinghouse
/// submitter behind <see cref="IClaimSubmitter"/>.
/// </summary>
public sealed class StubClaimSubmitter : IClaimSubmitter
{
    public ValueTask<string> SubmitAsync(ClaimDetailDto claim, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(claim);
        var control = $"STUB-{claim.Id.ToString("N")[..8].ToUpperInvariant()}";
        return ValueTask.FromResult(control);
    }
}
```

- [ ] **Step 4: Register in `ClaimsModule.ConfigureServices`**

In `ClaimsModule.cs`, add under the DbContext registration (replacing the Task-3 comment placeholder):
```csharp
        builder.Services.AddScoped<Submission.IClaimSubmitter, Submission.StubClaimSubmitter>();
```

- [ ] **Step 5: Run — verify green + build**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter StubClaimSubmitterTests`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Claims src/Tests/Claims.Tests
git commit -m "feat(claims): stub claim submitter behind IClaimSubmitter seam"
```

---

## Task 7: Transition command handlers + validators + endpoints (TDD)

**Files:** (per transition: `Handler.cs`, `Validator.cs`, `Endpoint.cs` under `Features/v1/Claims/<Name>/`)
- MarkReady, Submit, MarkPaid, MarkDenied, Void
- Create: `src/Modules/Claims/Modules.Claims/Features/v1/Claims/ClaimMappings.cs`
- Create: `src/Tests/Claims.Tests/Features/ClaimTransitionHandlerTests.cs`
- Create: `src/Tests/Claims.Tests/Validators/ClaimValidatorsTests.cs`
- Modify: `ClaimsModule.MapEndpoints` (add the five `group.Map...Endpoint()` calls)

**Interfaces:**
- Consumes: the five commands (Task 4), `ClaimsDbContext`, `IClaimSubmitter`, `Claim`.
- Produces: `ClaimMappings.ToDetailDto(this Claim)` → `ClaimDetailDto`; five `public sealed` handlers returning `ValueTask<Guid>`; five validators named `<Command>Validator`; five endpoint extension methods `Map<Name>Endpoint(this IEndpointRouteBuilder)`.

- [ ] **Step 1: Write `ClaimMappings`** (needed by Submit handler + Task 8)

```csharp
using FSH.Modules.Claims.Contracts;
using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Domain;

namespace FSH.Modules.Claims.Features.v1.Claims;

internal static class ClaimMappings
{
    public static ClaimDetailDto ToDetailDto(this Claim c) => new(
        c.Id, c.SuperBillId, c.ReportId, c.PatientId, c.InsuranceTypeId,
        (ClaimStatus)(int)c.Status, c.TotalCharge, c.ControlNumber,
        c.CreatedAtUtc, c.UpdatedAtUtc, c.SubmittedAtUtc, c.ResolvedAtUtc,
        c.Lines.Select(l => new ClaimLineDto(l.ProcedureCodeId, l.Code, l.Description, l.Charge, l.DiagnosticIds)).ToList());

    public static ClaimListItemDto ToListItemDto(this Claim c) => new(
        c.Id, c.SuperBillId, c.ReportId, c.PatientId, c.InsuranceTypeId,
        (ClaimStatus)(int)c.Status, c.TotalCharge, c.Lines.Count,
        c.CreatedAtUtc, c.SubmittedAtUtc, c.ResolvedAtUtc);
}
```

> The `(ClaimStatus)(int)` cast maps the runtime `Domain.ClaimStatus` to the identically-ordered `Contracts.ClaimStatus`. Both enums MUST keep the same member order (Draft=0…Voided=5).

- [ ] **Step 2: Write the failing handler + validator tests**

```csharp
// src/Tests/Claims.Tests/Features/ClaimTransitionHandlerTests.cs
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Claims.Features.v1.Claims.MarkReady;
using FSH.Modules.Claims.Features.v1.Claims.Submit;
using FSH.Modules.Claims.Features.v1.Claims.MarkPaid;
using FSH.Modules.Claims.Submission;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Claims.Tests.Features;

public sealed class ClaimTransitionHandlerTests
{
    private static ClaimsDbContext Ctx(string name)
    {
        var options = new DbContextOptionsBuilder<ClaimsDbContext>().UseInMemoryDatabase(name).Options;
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(
            new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));
        var settings = Options.Create(new DatabaseOptions
        { Provider = "postgresql", ConnectionString = string.Empty, MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL" });
        return new ClaimsDbContext(accessor, options, settings, Substitute.For<IHostEnvironment>());
    }

    private static async Task<Claim> SeedDraft(ClaimsDbContext db)
    {
        var claim = Claim.CreateFromSuperBill(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, false,
            [ClaimLine.Create(Guid.Empty, Guid.NewGuid(), "99213", "Visit", 100m, [])]);
        db.Claims.Add(claim);
        await db.SaveChangesAsync();
        return claim;
    }

    [Fact]
    public async Task MarkReady_Advances_To_Ready()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        var sut = new MarkClaimReadyCommandHandler(db);

        await sut.Handle(new MarkClaimReadyCommand(claim.Id), CancellationToken.None);

        (await db.Claims.FindAsync(claim.Id))!.Status.ShouldBe(ClaimStatus.Ready);
    }

    [Fact]
    public async Task Submit_Calls_Submitter_And_Sets_ControlNumber()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        await db.SaveChangesAsync();

        var submitter = Substitute.For<IClaimSubmitter>();
        submitter.SubmitAsync(Arg.Any<FSH.Modules.Claims.Contracts.Dtos.ClaimDetailDto>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("CTRL-9"));
        var sut = new SubmitClaimCommandHandler(db, submitter);

        await sut.Handle(new SubmitClaimCommand(claim.Id), CancellationToken.None);

        var saved = await db.Claims.FindAsync(claim.Id);
        saved!.Status.ShouldBe(ClaimStatus.Submitted);
        saved.ControlNumber.ShouldBe("CTRL-9");
    }

    [Fact]
    public async Task MarkPaid_From_Ready_Throws_InvalidOperation()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var claim = await SeedDraft(db);
        claim.MarkReady();
        await db.SaveChangesAsync();
        var sut = new MarkClaimPaidCommandHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(
            () => sut.Handle(new MarkClaimPaidCommand(claim.Id), CancellationToken.None).AsTask());
    }
}
```

```csharp
// src/Tests/Claims.Tests/Validators/ClaimValidatorsTests.cs
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Features.v1.Claims.MarkReady;
using FSH.Modules.Claims.Features.v1.Claims.GetClaims;
using Shouldly;
using Xunit;

namespace Claims.Tests.Validators;

public sealed class ClaimValidatorsTests
{
    [Fact]
    public void MarkReady_Rejects_Empty_Id()
    {
        var result = new MarkClaimReadyCommandValidator().Validate(new MarkClaimReadyCommand(Guid.Empty));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void GetClaims_Rejects_Bad_Paging()
    {
        var result = new GetClaimsQueryValidator().Validate(new GetClaimsQuery(PageNumber: 0, PageSize: 5000));
        result.IsValid.ShouldBeFalse();
    }
}
```

- [ ] **Step 3: Run — verify fail (handlers/validators missing)**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter "ClaimTransitionHandlerTests|ClaimValidatorsTests"`
Expected: FAIL — types not found.

- [ ] **Step 4: Write MarkReady (handler + validator + endpoint)**

```csharp
// Features/v1/Claims/MarkReady/MarkClaimReadyCommandHandler.cs
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkReady;

public sealed class MarkClaimReadyCommandHandler(ClaimsDbContext db)
    : ICommandHandler<MarkClaimReadyCommand, Guid>
{
    public async ValueTask<Guid> Handle(MarkClaimReadyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");
        claim.MarkReady();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
```
```csharp
// Features/v1/Claims/MarkReady/MarkClaimReadyCommandValidator.cs
using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkReady;

public sealed class MarkClaimReadyCommandValidator : AbstractValidator<MarkClaimReadyCommand>
{
    public MarkClaimReadyCommandValidator() =>
        RuleFor(x => x.ClaimId).NotEmpty();
}
```
```csharp
// Features/v1/Claims/MarkReady/MarkClaimReadyEndpoint.cs
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkReady;

public static class MarkClaimReadyEndpoint
{
    internal static RouteHandlerBuilder MapMarkClaimReadyEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/ready",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new MarkClaimReadyCommand(claimId), ct)))
            .WithName("MarkClaimReady")
            .WithSummary("Mark a draft claim ready to submit")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
```

- [ ] **Step 5: Write Submit (handler loads detail DTO → calls submitter → `claim.Submit`)**

```csharp
// Features/v1/Claims/Submit/SubmitClaimCommandHandler.cs
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Features.v1.Claims; // ClaimMappings
using FSH.Modules.Claims.Submission;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.Submit;

public sealed class SubmitClaimCommandHandler(ClaimsDbContext db, IClaimSubmitter submitter)
    : ICommandHandler<SubmitClaimCommand, Guid>
{
    public async ValueTask<Guid> Handle(SubmitClaimCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");

        var control = await submitter.SubmitAsync(claim.ToDetailDto(), cancellationToken).ConfigureAwait(false);
        claim.Submit(control);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
```
```csharp
// Features/v1/Claims/Submit/SubmitClaimCommandValidator.cs
using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.Submit;

public sealed class SubmitClaimCommandValidator : AbstractValidator<SubmitClaimCommand>
{
    public SubmitClaimCommandValidator() => RuleFor(x => x.ClaimId).NotEmpty();
}
```
```csharp
// Features/v1/Claims/Submit/SubmitClaimEndpoint.cs
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.Submit;

public static class SubmitClaimEndpoint
{
    internal static RouteHandlerBuilder MapSubmitClaimEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/submit",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new SubmitClaimCommand(claimId), ct)))
            .WithName("SubmitClaim")
            .WithSummary("Submit a ready claim to the clearinghouse (stub)")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
```

- [ ] **Step 6: Write MarkPaid, MarkDenied, Void (three handlers + three validators + three endpoints)**

MarkPaid:
```csharp
// Features/v1/Claims/MarkPaid/MarkClaimPaidCommandHandler.cs
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkPaid;

public sealed class MarkClaimPaidCommandHandler(ClaimsDbContext db)
    : ICommandHandler<MarkClaimPaidCommand, Guid>
{
    public async ValueTask<Guid> Handle(MarkClaimPaidCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");
        claim.MarkPaid();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
```
```csharp
// Features/v1/Claims/MarkPaid/MarkClaimPaidCommandValidator.cs
using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;
namespace FSH.Modules.Claims.Features.v1.Claims.MarkPaid;
public sealed class MarkClaimPaidCommandValidator : AbstractValidator<MarkClaimPaidCommand>
{
    public MarkClaimPaidCommandValidator() => RuleFor(x => x.ClaimId).NotEmpty();
}
```
```csharp
// Features/v1/Claims/MarkPaid/MarkClaimPaidEndpoint.cs
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkPaid;

public static class MarkClaimPaidEndpoint
{
    internal static RouteHandlerBuilder MapMarkClaimPaidEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/paid",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new MarkClaimPaidCommand(claimId), ct)))
            .WithName("MarkClaimPaid")
            .WithSummary("Mark a submitted claim paid")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
```

MarkDenied (identical shape; route `/{claimId:guid}/denied`, name `MarkClaimDenied`, command `MarkClaimDeniedCommand`, aggregate call `claim.MarkDenied()`):
```csharp
// Features/v1/Claims/MarkDenied/MarkClaimDeniedCommandHandler.cs
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkDenied;

public sealed class MarkClaimDeniedCommandHandler(ClaimsDbContext db)
    : ICommandHandler<MarkClaimDeniedCommand, Guid>
{
    public async ValueTask<Guid> Handle(MarkClaimDeniedCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");
        claim.MarkDenied();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
```
```csharp
// Features/v1/Claims/MarkDenied/MarkClaimDeniedCommandValidator.cs
using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;
namespace FSH.Modules.Claims.Features.v1.Claims.MarkDenied;
public sealed class MarkClaimDeniedCommandValidator : AbstractValidator<MarkClaimDeniedCommand>
{
    public MarkClaimDeniedCommandValidator() => RuleFor(x => x.ClaimId).NotEmpty();
}
```
```csharp
// Features/v1/Claims/MarkDenied/MarkClaimDeniedEndpoint.cs
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkDenied;

public static class MarkClaimDeniedEndpoint
{
    internal static RouteHandlerBuilder MapMarkClaimDeniedEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/denied",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new MarkClaimDeniedCommand(claimId), ct)))
            .WithName("MarkClaimDenied")
            .WithSummary("Mark a submitted claim denied")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
```

Void (carries an optional reason; route `/{claimId:guid}/void`):
```csharp
// Features/v1/Claims/Void/VoidClaimCommandHandler.cs
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.Void;

public sealed class VoidClaimCommandHandler(ClaimsDbContext db)
    : ICommandHandler<VoidClaimCommand, Guid>
{
    public async ValueTask<Guid> Handle(VoidClaimCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var claim = await db.Claims.FirstOrDefaultAsync(c => c.Id == command.ClaimId, cancellationToken)
            .ConfigureAwait(false) ?? throw new NotFoundException($"Claim {command.ClaimId} not found.");
        claim.Void();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return claim.Id;
    }
}
```
```csharp
// Features/v1/Claims/Void/VoidClaimCommandValidator.cs
using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;
namespace FSH.Modules.Claims.Features.v1.Claims.Void;
public sealed class VoidClaimCommandValidator : AbstractValidator<VoidClaimCommand>
{
    public VoidClaimCommandValidator()
    {
        RuleFor(x => x.ClaimId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(512);
    }
}
```
```csharp
// Features/v1/Claims/Void/VoidClaimEndpoint.cs
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.Void;

public static class VoidClaimEndpoint
{
    internal static RouteHandlerBuilder MapVoidClaimEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/void",
                async (Guid claimId, VoidClaimRequest? body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new VoidClaimCommand(claimId, body?.Reason), ct)))
            .WithName("VoidClaim")
            .WithSummary("Void a non-terminal claim")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}

public sealed record VoidClaimRequest(string? Reason);
```

- [ ] **Step 7: Wire the five endpoints into `ClaimsModule.MapEndpoints`**

Replace the Task-3 placeholder comment block with:
```csharp
        group.MapMarkClaimReadyEndpoint();
        group.MapSubmitClaimEndpoint();
        group.MapMarkClaimPaidEndpoint();
        group.MapMarkClaimDeniedEndpoint();
        group.MapVoidClaimEndpoint();
```
Add the matching `using FSH.Modules.Claims.Features.v1.Claims.MarkReady;` (etc.) at the top of `ClaimsModule.cs`.

- [ ] **Step 8: Run tests — verify green**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter "ClaimTransitionHandlerTests|ClaimValidatorsTests"`
Expected: PASS.

- [ ] **Step 9: Build the whole solution (catches endpoint wiring / warnings)**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Claims src/Tests/Claims.Tests
git commit -m "feat(claims): transition commands (ready/submit/paid/denied/void) + endpoints"
```

---

## Task 8: Query handlers — GetClaims (paginated worklist + KPIs) & GetClaimById (TDD)

**Files:**
- Create: `Features/v1/Claims/GetClaims/GetClaimsQueryHandler.cs`
- Create: `Features/v1/Claims/GetClaims/GetClaimsQueryValidator.cs`
- Create: `Features/v1/Claims/GetClaims/GetClaimsEndpoint.cs`
- Create: `Features/v1/Claims/GetClaimById/GetClaimByIdQueryHandler.cs`
- Create: `Features/v1/Claims/GetClaimById/GetClaimByIdEndpoint.cs`
- Create: `src/Tests/Claims.Tests/Features/GetClaimsQueryHandlerTests.cs`
- Modify: `ClaimsModule.MapEndpoints` (add the two query endpoints)

**Interfaces:**
- Consumes: `GetClaimsQuery`, `GetClaimByIdQuery`, `ClaimsDbContext`, `ClaimMappings`.
- Produces: `GetClaimsQueryHandler : IQueryHandler<GetClaimsQuery, ClaimsPageDto>`; `GetClaimByIdQueryHandler : IQueryHandler<GetClaimByIdQuery, ClaimDetailDto>`; `GetClaimsQueryValidator`; endpoints `MapGetClaimsEndpoint`, `MapGetClaimByIdEndpoint`.

- [ ] **Step 1: Write the failing query test**

```csharp
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Contracts;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Claims.Features.v1.Claims.GetClaims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Claims.Tests.Features;

public sealed class GetClaimsQueryHandlerTests
{
    private static ClaimsDbContext Ctx(string name)
    {
        var options = new DbContextOptionsBuilder<ClaimsDbContext>().UseInMemoryDatabase(name).Options;
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(
            new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));
        var settings = Options.Create(new DatabaseOptions
        { Provider = "postgresql", ConnectionString = string.Empty, MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL" });
        return new ClaimsDbContext(accessor, options, settings, Substitute.For<IHostEnvironment>());
    }

    private static Claim Draft(decimal charge) => Claim.CreateFromSuperBill(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, false,
        [ClaimLine.Create(Guid.Empty, Guid.NewGuid(), "99213", "Visit", charge, [])]);

    [Fact]
    public async Task Returns_Page_And_Summary_With_Outstanding()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var ready = Draft(100m); ready.MarkReady();
        var submitted = Draft(200m); submitted.MarkReady(); submitted.Submit("C");
        db.Claims.AddRange(Draft(50m), ready, submitted);
        await db.SaveChangesAsync();

        var sut = new GetClaimsQueryHandler(db);
        var result = await sut.Handle(new GetClaimsQuery(), CancellationToken.None);

        result.Page.TotalCount.ShouldBe(3);
        result.Summary.Draft.ShouldBe(1);
        result.Summary.Ready.ShouldBe(1);
        result.Summary.Submitted.ShouldBe(1);
        result.Summary.OutstandingCharge.ShouldBe(300m); // ready 100 + submitted 200
    }

    [Fact]
    public async Task Filters_By_Status()
    {
        using var db = Ctx(Guid.NewGuid().ToString());
        var ready = Draft(100m); ready.MarkReady();
        db.Claims.AddRange(Draft(50m), ready);
        await db.SaveChangesAsync();

        var sut = new GetClaimsQueryHandler(db);
        var result = await sut.Handle(new GetClaimsQuery(Status: ClaimStatus.Ready), CancellationToken.None);

        result.Page.Items.Count.ShouldBe(1);
        result.Page.Items[0].Status.ShouldBe(ClaimStatus.Ready);
    }
}
```

- [ ] **Step 2: Run — verify fail**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter GetClaimsQueryHandlerTests`
Expected: FAIL — handler not found.

- [ ] **Step 3: Write `GetClaimsQueryValidator`**

```csharp
using FluentValidation;
using FSH.Modules.Claims.Contracts.v1.Claims;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaims;

public sealed class GetClaimsQueryValidator : AbstractValidator<GetClaimsQuery>
{
    public GetClaimsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(128);
    }
}
```

- [ ] **Step 4: Write `GetClaimsQueryHandler`** (tenant filtering is automatic via `BaseDbContext` query filter; compute summary over the full filtered set, page the items)

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Contracts;
using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaims;

public sealed class GetClaimsQueryHandler(ClaimsDbContext db)
    : IQueryHandler<GetClaimsQuery, ClaimsPageDto>
{
    public async ValueTask<ClaimsPageDto> Handle(GetClaimsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Base query (tenant-filtered by BaseDbContext) BEFORE status filter — summary spans all statuses.
        var baseQuery = db.Claims.AsNoTracking();
        if (query.InsuranceTypeId is { } payer)
        {
            baseQuery = baseQuery.Where(c => c.InsuranceTypeId == payer);
        }

        // Summary over the (payer-filtered) set, independent of the status filter + paging.
        var counts = await baseQuery
            .GroupBy(c => c.Status)
            .Select(g => new { g.Key, Count = g.Count(), Charge = g.Sum(x => x.TotalCharge) })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        int CountOf(ClaimStatus s) => counts.FirstOrDefault(c => c.Key == s)?.Count ?? 0;
        decimal ChargeOf(ClaimStatus s) => counts.FirstOrDefault(c => c.Key == s)?.Charge ?? 0m;

        var summary = new ClaimsSummaryDto(
            Draft: CountOf(ClaimStatus.Draft),
            Ready: CountOf(ClaimStatus.Ready),
            Submitted: CountOf(ClaimStatus.Submitted),
            Paid: CountOf(ClaimStatus.Paid),
            Denied: CountOf(ClaimStatus.Denied),
            OutstandingCharge: ChargeOf(ClaimStatus.Ready) + ChargeOf(ClaimStatus.Submitted));

        // Rows: apply status filter + paging.
        var rows = baseQuery.Include(c => c.Lines);
        var filtered = query.Status is { } st
            ? rows.Where(c => c.Status == st)
            : rows;

        var total = await filtered.LongCountAsync(cancellationToken).ConfigureAwait(false);
        var items = await filtered
            .OrderByDescending(c => c.CreatedAtUtc)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var page = new PagedResponse<ClaimListItemDto>
        {
            Items = items.Select(c => c.ToListItemDto()).ToList(),
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)query.PageSize),
        };

        return new ClaimsPageDto(page, summary);
    }
}
```

> If `PagedResponse<T>` in this repo uses constructor args rather than init properties, match its actual shape (see `src/BuildingBlocks/Shared/Persistence/PagedResponse.cs` — the `GetInvoicesQueryHandler` uses object-initializer form, mirrored here).

- [ ] **Step 5: Write `GetClaimsEndpoint`**

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Claims.Contracts;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaims;

public static class GetClaimsEndpoint
{
    internal static RouteHandlerBuilder MapGetClaimsEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/",
                (ClaimStatus? status, Guid? insuranceTypeId, string? search,
                 int pageNumber, int pageSize, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetClaimsQuery(
                        status, insuranceTypeId, search,
                        pageNumber <= 0 ? 1 : pageNumber,
                        pageSize <= 0 ? 20 : Math.Min(pageSize, 100)), ct))
            .WithName("GetClaims")
            .WithSummary("List claims (worklist) with status summary")
            .RequirePermission(ClaimsPermissions.View);
}
```

- [ ] **Step 6: Write `GetClaimByIdQueryHandler` + endpoint**

```csharp
// Features/v1/Claims/GetClaimById/GetClaimByIdQueryHandler.cs
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Claims.Contracts.Dtos;
using FSH.Modules.Claims.Contracts.v1.Claims;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Features.v1.Claims; // ClaimMappings
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaimById;

public sealed class GetClaimByIdQueryHandler(ClaimsDbContext db)
    : IQueryHandler<GetClaimByIdQuery, ClaimDetailDto>
{
    public async ValueTask<ClaimDetailDto> Handle(GetClaimByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var claim = await db.Claims.AsNoTracking().Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == query.ClaimId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException($"Claim {query.ClaimId} not found.");
        return claim.ToDetailDto();
    }
}
```
```csharp
// Features/v1/Claims/GetClaimById/GetClaimByIdEndpoint.cs
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaimById;

public static class GetClaimByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetClaimByIdEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{claimId:guid}",
                (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetClaimByIdQuery(claimId), ct))
            .WithName("GetClaimById")
            .WithSummary("Get a claim with its snapshot lines")
            .RequirePermission(ClaimsPermissions.View);
}
```

- [ ] **Step 7: Wire query endpoints into `ClaimsModule.MapEndpoints`** (add above the transition mappings)

```csharp
        group.MapGetClaimsEndpoint();
        group.MapGetClaimByIdEndpoint();
```
Add `using FSH.Modules.Claims.Features.v1.Claims.GetClaims;` and `...GetClaimById;`.

- [ ] **Step 8: Run tests + build**

Run: `dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj --filter GetClaimsQueryHandlerTests`
Expected: PASS.
Run: `dotnet build src/FSH.Starter.slnx`
Expected: Build succeeded, 0 warnings.

- [ ] **Step 9: Run the Architecture tests (validator pairing + module boundaries)**

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj`
Expected: PASS — confirms every new command/paginated-query handler has a validator and Claims only depends on `.Contracts` projects.

- [ ] **Step 10: Commit**

```bash
git add src/Modules/Claims src/Tests/Claims.Tests
git commit -m "feat(claims): worklist query with KPI summary + claim-by-id query"
```

---

## Task 9: Frontend API module (`api/claims.ts`)

**Files:**
- Create: `clients/dashboard/src/api/claims.ts`

**Interfaces:**
- Consumes: `apiFetch` (`@/lib/api-client`), `PagedResult<T>` shape (mirror `api/billing.ts`).
- Produces: TS types `ClaimStatus`, `ClaimLineDto`, `ClaimListItemDto`, `ClaimDetailDto`, `ClaimsSummaryDto`, `ClaimsPageDto`; functions `getClaims(params)`, `getClaim(id)`, `markClaimReady(id)`, `submitClaim(id)`, `markClaimPaid(id)`, `markClaimDenied(id)`, `voidClaim({id, reason})`.

- [ ] **Step 1: Write `api/claims.ts`**

```ts
import { apiFetch } from "@/lib/api-client";

export type ClaimStatus =
  | "Draft" | "Ready" | "Submitted" | "Paid" | "Denied" | "Voided" | (string & {});

export type ClaimLineDto = {
  procedureCodeId: string;
  code: string;
  description?: string | null;
  charge: number;
  diagnosticIds: string[];
};

export type ClaimListItemDto = {
  id: string;
  superBillId: string;
  reportId: string;
  patientId: string;
  insuranceTypeId?: string | null;
  status: ClaimStatus;
  totalCharge: number;
  lineCount: number;
  createdAtUtc: string;
  submittedAtUtc?: string | null;
  resolvedAtUtc?: string | null;
};

export type ClaimDetailDto = {
  id: string;
  superBillId: string;
  reportId: string;
  patientId: string;
  insuranceTypeId?: string | null;
  status: ClaimStatus;
  totalCharge: number;
  controlNumber?: string | null;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
  submittedAtUtc?: string | null;
  resolvedAtUtc?: string | null;
  lines: ClaimLineDto[];
};

export type ClaimsSummaryDto = {
  draft: number;
  ready: number;
  submitted: number;
  paid: number;
  denied: number;
  outstandingCharge: number;
};

export type PagedResult<T> = {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
};

export type ClaimsPageDto = {
  page: PagedResult<ClaimListItemDto>;
  summary: ClaimsSummaryDto;
};

export type ClaimSearchParams = {
  status?: ClaimStatus;
  insuranceTypeId?: string;
  search?: string;
  pageNumber?: number;
  pageSize?: number;
};

export function getClaims(params: ClaimSearchParams = {}) {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.insuranceTypeId) query.set("insuranceTypeId", params.insuranceTypeId);
  if (params.search) query.set("search", params.search);
  if (params.pageNumber) query.set("pageNumber", String(params.pageNumber));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return apiFetch<ClaimsPageDto>(`/api/v1/claims${suffix}`);
}

export function getClaim(id: string) {
  return apiFetch<ClaimDetailDto>(`/api/v1/claims/${id}`);
}

const post = (id: string, action: string) =>
  apiFetch<string>(`/api/v1/claims/${id}/${action}`, { method: "POST" });

export const markClaimReady = (id: string) => post(id, "ready");
export const submitClaim = (id: string) => post(id, "submit");
export const markClaimPaid = (id: string) => post(id, "paid");
export const markClaimDenied = (id: string) => post(id, "denied");

export function voidClaim({ id, reason }: { id: string; reason?: string }) {
  return apiFetch<string>(`/api/v1/claims/${id}/void`, {
    method: "POST",
    body: JSON.stringify({ reason: reason ?? null }),
  });
}
```

> Verify `apiFetch`'s options signature (method/body) against `@/lib/api-client` and an existing POST call (e.g. how `tickets.ts` or `report-procedures.ts` issue writes); match it exactly (it may already set JSON content-type + auth/tenant headers).

- [ ] **Step 2: Typecheck**

Run: `cd clients/dashboard && npx tsc -b`
Expected: no errors.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/src/api/claims.ts
git commit -m "feat(dashboard): claims api client"
```

---

## Task 10: Frontend worklist page + nav + route

**Files:**
- Create: `clients/dashboard/src/pages/billing/claims-list.tsx`
- Modify: `clients/dashboard/src/components/layout/nav-data.ts` (add "Billing" section)
- Modify: `clients/dashboard/src/routes.tsx` (lazy import + routes)

**Interfaces:**
- Consumes: `getClaims`, `ClaimStatus`, `ClaimListItemDto`, `ClaimsSummaryDto` (Task 9). For name resolution (verified APIs): `useInsuranceTypeOptions()` from `@/api/administration` returns `ComboboxOption[] | undefined` **directly** (`{ value: id, label: name }`) — no `.data`; `getPatientById(id)` from `@/api/patients` returns `PatientDetailDto` with `.demographics.firstName`/`.lastName` (there is **no** `usePatients` hook). Resolve the visible page's patient ids via `useQueries` + `getPatientById` (TanStack-cached).
- Produces: `export function ClaimsListPage()`; a shared `<ClaimStatusPill status=.../>` component (co-located or in `@/components`), reused by Task 11.

- [ ] **Step 1: Add the "Billing" nav section** in `nav-data.ts`

Add `ReceiptText` to the lucide import, and insert a new section (place it after the `patients` section so clinical work leads, before `helpdesk`):
```ts
  {
    id: "billing",
    caption: "Billing",
    icon: ReceiptText,
    items: [
      { to: "/billing/claims", label: "Claims", icon: ReceiptText, perm: "Permissions.Claims.View" },
    ],
  },
```

- [ ] **Step 2: Register lazy route + paths** in `routes.tsx`

Add the lazy import near the other page imports:
```ts
const ClaimsListPage = lazyNamed(() => import("@/pages/billing/claims-list"), "ClaimsListPage");
const ClaimDetailPage = lazyNamed(() => import("@/pages/billing/claim-detail"), "ClaimDetailPage");
```
Add child routes inside the `AppShell` children array (next to `invoices`):
```ts
          { path: "billing/claims", element: withSuspense(<ClaimsListPage />) },
          { path: "billing/claims/:claimId", element: withSuspense(<ClaimDetailPage />) },
```
(`ClaimDetailPage` is authored in Task 11; the import compiles now because the module will exist after Task 11 — implement Task 10 and Task 11 together before running the dev server, or stub `claim-detail.tsx` with an empty `export function ClaimDetailPage() { return null; }` and flesh it out in Task 11.)

- [ ] **Step 3: Write the worklist page** (table + KPI strip; the chosen layout). Match existing page structure — copy the shell/header/table imports from `@/pages/invoices` or `@/pages/tickets/tickets`. Resolve patient & payer names client-side.

```tsx
import { useMemo, useState } from "react";
import { useQuery, useQueries } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { getClaims, type ClaimStatus, type ClaimListItemDto } from "@/api/claims";
import { getPatientById } from "@/api/patients";
import { useInsuranceTypeOptions } from "@/api/administration";

const STATUSES: ClaimStatus[] = ["Draft", "Ready", "Submitted", "Paid", "Denied", "Voided"];

export function ClaimStatusPill({ status }: { status: ClaimStatus }) {
  // Billing accent + per-status colors; frontend-design skill refines the exact palette.
  const cls: Record<string, string> = {
    Draft: "bg-slate-200 text-slate-700",
    Ready: "bg-teal-100 text-teal-800",
    Submitted: "bg-blue-100 text-blue-800",
    Paid: "bg-green-100 text-green-800",
    Denied: "bg-red-100 text-red-800",
    Voided: "bg-slate-100 text-slate-500 line-through",
  };
  return (
    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-semibold ${cls[status] ?? cls.Draft}`}>
      {status}
    </span>
  );
}

export function ClaimsListPage() {
  const [status, setStatus] = useState<ClaimStatus | undefined>(undefined);
  const [page, setPage] = useState(1);

  const claimsQuery = useQuery({
    queryKey: ["claims", { status, page }],
    queryFn: () => getClaims({ status, pageNumber: page, pageSize: 20 }),
  });

  const summary = claimsQuery.data?.summary;
  const items: ClaimListItemDto[] = claimsQuery.data?.page.items ?? [];

  // Payer names: useInsuranceTypeOptions() returns ComboboxOption[] (value=id, label=name) directly.
  const insuranceOptions = useInsuranceTypeOptions();
  const payerName = useMemo(() => {
    const map = new Map((insuranceOptions ?? []).map((o) => [o.value, o.label]));
    return (id?: string | null) => (id ? map.get(id) ?? "—" : "—");
  }, [insuranceOptions]);

  // Patient names: resolve the visible page's ids via getPatientById (TanStack-cached, deduped).
  const patientIds = useMemo(
    () => Array.from(new Set(items.map((c) => c.patientId))),
    [items],
  );
  const patientQueries = useQueries({
    queries: patientIds.map((id) => ({
      queryKey: ["patient", id],
      queryFn: () => getPatientById(id),
      staleTime: 5 * 60_000,
    })),
  });
  const patientName = useMemo(() => {
    const map = new Map<string, string>();
    patientQueries.forEach((q, i) => {
      const p = q.data;
      if (p) {
        map.set(patientIds[i], `${p.demographics.lastName}, ${p.demographics.firstName}`.trim());
      }
    });
    return (id: string) => map.get(id) ?? "—";
  }, [patientQueries, patientIds]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold">Claims</h1>
      </div>

      {/* KPI strip */}
      <div className="grid grid-cols-4 gap-3">
        <Kpi label="Draft" value={summary?.draft ?? 0} />
        <Kpi label="Ready" value={summary?.ready ?? 0} />
        <Kpi label="Submitted" value={summary?.submitted ?? 0} />
        <Kpi label="Outstanding" value={`$${(summary?.outstandingCharge ?? 0).toLocaleString()}`} accent />
      </div>

      {/* Status filter */}
      <div className="flex gap-2">
        <FilterChip active={!status} onClick={() => { setStatus(undefined); setPage(1); }}>All</FilterChip>
        {STATUSES.map((s) => (
          <FilterChip key={s} active={status === s} onClick={() => { setStatus(s); setPage(1); }}>{s}</FilterChip>
        ))}
      </div>

      {/* Table */}
      <div className="overflow-x-auto rounded-lg border">
        <table className="w-full text-sm">
          <thead className="text-left text-xs uppercase text-muted-foreground">
            <tr>
              <th className="p-2">Patient</th><th className="p-2">Payer</th>
              <th className="p-2">CPT</th><th className="p-2">Charge</th><th className="p-2">Status</th>
            </tr>
          </thead>
          <tbody>
            {items.map((c) => (
              <tr key={c.id} className="border-t hover:bg-muted/40">
                <td className="p-2">
                  <Link className="font-medium hover:underline" to={`/billing/claims/${c.id}`}>
                    {patientName(c.patientId)}
                  </Link>
                </td>
                <td className="p-2">{payerName(c.insuranceTypeId)}</td>
                <td className="p-2">{c.lineCount}</td>
                <td className="p-2">${c.totalCharge.toLocaleString()}</td>
                <td className="p-2"><ClaimStatusPill status={c.status} /></td>
              </tr>
            ))}
            {items.length === 0 && !claimsQuery.isLoading && (
              <tr><td colSpan={5} className="p-6 text-center text-muted-foreground">No claims yet.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function Kpi({ label, value, accent }: { label: string; value: string | number; accent?: boolean }) {
  return (
    <div className="rounded-lg border p-3">
      <div className={`text-2xl font-bold ${accent ? "text-teal-600" : ""}`}>{value}</div>
      <div className="text-xs text-muted-foreground">{label}</div>
    </div>
  );
}

function FilterChip({ active, onClick, children }: { active: boolean; onClick: () => void; children: React.ReactNode }) {
  return (
    <button type="button" onClick={onClick}
      className={`rounded-full border px-3 py-1 text-xs ${active ? "border-teal-500 bg-teal-50 text-teal-800" : ""}`}>
      {children}
    </button>
  );
}
```

> This uses raw Tailwind for the mockup structure. During implementation, invoke the **frontend-design** skill to align components (table, chips, KPI tiles, pill palette, empty/loading states) with the dashboard's shadcn primitives and the Billing teal accent. The name-resolution hooks above are the verified real APIs (`useInsuranceTypeOptions` returns `ComboboxOption[]`; `getPatientById` via `useQueries`). Per-page `getPatientById` fan-out (~20 cached calls) is acceptable for this slice; if it proves heavy, add a bulk "names by ids" endpoint in a follow-up (note it in the PR).

- [ ] **Step 4: Typecheck + lint**

Run: `cd clients/dashboard && npx tsc -b && npm run lint`
Expected: clean.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/pages/billing/claims-list.tsx clients/dashboard/src/components/layout/nav-data.ts clients/dashboard/src/routes.tsx
git commit -m "feat(dashboard): Billing menu + claims worklist (table + KPI strip)"
```

---

## Task 11: Frontend claim detail page (two-column + sticky action bar)

**Files:**
- Create/replace: `clients/dashboard/src/pages/billing/claim-detail.tsx`

**Interfaces:**
- Consumes: `getClaim`, `markClaimReady`, `submitClaim`, `markClaimPaid`, `markClaimDenied`, `voidClaim` (Task 9); `ClaimStatusPill` (Task 10); patient/payer name hooks.
- Produces: `export function ClaimDetailPage()`.

- [ ] **Step 1: Write the detail page** (two-column; lifecycle buttons enabled by status; mutations pass data via `mutate(arg)`)

```tsx
import { useParams, Link } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getClaim, markClaimReady, submitClaim, markClaimPaid, markClaimDenied, voidClaim,
  type ClaimStatus,
} from "@/api/claims";
import { ClaimStatusPill } from "@/pages/billing/claims-list";

export function ClaimDetailPage() {
  const { claimId = "" } = useParams();
  const qc = useQueryClient();
  const claimQuery = useQuery({ queryKey: ["claim", claimId], queryFn: () => getClaim(claimId) });

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: ["claim", claimId] });
    qc.invalidateQueries({ queryKey: ["claims"] });
  };
  const ready = useMutation({ mutationFn: markClaimReady, onSuccess: invalidate });
  const submit = useMutation({ mutationFn: submitClaim, onSuccess: invalidate });
  const paid = useMutation({ mutationFn: markClaimPaid, onSuccess: invalidate });
  const denied = useMutation({ mutationFn: markClaimDenied, onSuccess: invalidate });
  const doVoid = useMutation({ mutationFn: voidClaim, onSuccess: invalidate });

  const claim = claimQuery.data;
  if (!claim) return <div className="p-6 text-muted-foreground">Loading…</div>;

  const s: ClaimStatus = claim.status;
  const busy = ready.isPending || submit.isPending || paid.isPending || denied.isPending || doVoid.isPending;

  return (
    <div className="flex flex-col gap-4 pb-20">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-semibold">Claim</h1>
            <ClaimStatusPill status={s} />
          </div>
          <div className="text-sm text-muted-foreground">
            Source <Link className="text-teal-600 hover:underline" to={`/patient-charts/${claim.patientId}`}>report</Link>
            {claim.controlNumber ? ` · ${claim.controlNumber}` : ""}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-4">
        {/* Left: procedures + diagnoses */}
        <div className="col-span-2 space-y-4">
          <section className="rounded-lg border p-4">
            <h2 className="mb-2 text-xs font-bold uppercase text-muted-foreground">Procedures (snapshot)</h2>
            <table className="w-full text-sm">
              <tbody>
                {claim.lines.map((l) => (
                  <tr key={l.procedureCodeId} className="border-b last:border-0">
                    <td className="py-1">{l.code} · {l.description ?? ""}</td>
                    <td className="py-1 text-right font-medium">${l.charge.toLocaleString()}</td>
                  </tr>
                ))}
                <tr>
                  <td className="py-1 font-bold">Total</td>
                  <td className="py-1 text-right font-bold text-teal-600">${claim.totalCharge.toLocaleString()}</td>
                </tr>
              </tbody>
            </table>
          </section>
        </div>

        {/* Right: payer + activity rail */}
        <div className="space-y-4">
          <section className="rounded-lg border p-4">
            <h2 className="mb-2 text-xs font-bold uppercase text-muted-foreground">Payer</h2>
            <div className="text-sm">Insurance type: {claim.insuranceTypeId ?? "—"}</div>
          </section>
          <section className="rounded-lg border p-4 text-xs text-muted-foreground">
            <h2 className="mb-2 font-bold uppercase">Activity</h2>
            <div>Created {new Date(claim.createdAtUtc).toLocaleString()}</div>
            {claim.submittedAtUtc && <div>Submitted {new Date(claim.submittedAtUtc).toLocaleString()}</div>}
            {claim.resolvedAtUtc && <div>Resolved {new Date(claim.resolvedAtUtc).toLocaleString()}</div>}
          </section>
        </div>
      </div>

      {/* Sticky action bar — buttons enabled per legal transition */}
      <div className="fixed inset-x-0 bottom-0 flex justify-end gap-2 border-t bg-background/95 p-3">
        {s !== "Paid" && s !== "Denied" && s !== "Voided" && (
          <button type="button" disabled={busy} onClick={() => doVoid.mutate({ id: claim.id })}
            className="rounded-md border px-3 py-1.5 text-sm">Void</button>
        )}
        {s === "Draft" && (
          <button type="button" disabled={busy} onClick={() => ready.mutate(claim.id)}
            className="rounded-md bg-teal-600 px-3 py-1.5 text-sm font-semibold text-white">Mark Ready →</button>
        )}
        {s === "Ready" && (
          <button type="button" disabled={busy} onClick={() => submit.mutate(claim.id)}
            className="rounded-md bg-teal-600 px-3 py-1.5 text-sm font-semibold text-white">Submit →</button>
        )}
        {s === "Submitted" && (
          <>
            <button type="button" disabled={busy} onClick={() => denied.mutate(claim.id)}
              className="rounded-md border px-3 py-1.5 text-sm">Mark Denied</button>
            <button type="button" disabled={busy} onClick={() => paid.mutate(claim.id)}
              className="rounded-md bg-green-600 px-3 py-1.5 text-sm font-semibold text-white">Mark Paid</button>
          </>
        )}
      </div>
    </div>
  );
}
```

> Payer/patient names: resolve via the same hooks as Task 10 (shown raw here). During implementation, invoke **frontend-design** to align this with dashboard primitives + the teal accent, and add diagnosis-id resolution if the Administration diagnostics options are already loaded.

- [ ] **Step 2: Typecheck + lint**

Run: `cd clients/dashboard && npx tsc -b && npm run lint`
Expected: clean.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/src/pages/billing/claim-detail.tsx
git commit -m "feat(dashboard): claim detail page with lifecycle actions"
```

---

## Task 12: Playwright E2E (route-mocked)

**Files:**
- Create: `clients/dashboard/tests/billing/claims-worklist.spec.ts`

**Interfaces:**
- Consumes: the repo's Playwright harness helpers (verified in `tests/tickets/tickets-list.spec.ts`): `mockJsonResponse` from `../helpers/api-mocks`; `seedAuthedSession, TEST_USER` from `../helpers/auth-seed`; `installShellMocks, paged` from `../helpers/shell-mocks`. `installShellMocks` stubs the shell/permissions endpoints; `seedAuthedSession` signs in. Add `page.route` stubs for `/api/v1/claims*`, plus the patient-by-id and insurance-types endpoints the page calls for name resolution.

- [ ] **Step 1: Write the spec** (worklist renders + filter; open claim → Mark Ready calls the endpoint)

```ts
import { expect, test } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const claimsPage = {
  page: {
    items: [
      { id: "c1", superBillId: "s1", reportId: "r1", patientId: "p1", insuranceTypeId: "it1",
        status: "Draft", totalCharge: 420, lineCount: 3, createdAtUtc: new Date().toISOString(),
        submittedAtUtc: null, resolvedAtUtc: null },
    ],
    totalCount: 1, pageNumber: 1, pageSize: 20, totalPages: 1,
  },
  summary: { draft: 1, ready: 0, submitted: 0, paid: 0, denied: 0, outstandingCharge: 0 },
};

const claimDetail = {
  id: "c1", superBillId: "s1", reportId: "r1", patientId: "p1", insuranceTypeId: "it1",
  status: "Draft", totalCharge: 420, controlNumber: null, createdAtUtc: new Date().toISOString(),
  updatedAtUtc: null, submittedAtUtc: null, resolvedAtUtc: null,
  lines: [{ procedureCodeId: "pc1", code: "99213", description: "Office visit", charge: 420, diagnosticIds: [] }],
};

test.beforeEach(async ({ page }) => {
  await seedAuthedSession(page, { permissions: ["Permissions.Claims.View", "Permissions.Claims.Manage"] });
  await installShellMocks(page);
});

test.describe("Billing → Claims worklist", () => {
  test("renders worklist and advances a draft to Ready", async ({ page }) => {
    // Name-resolution endpoints the page calls (mock so hooks resolve):
    await page.route("**/api/v1/administration/insurance-types*", (route) =>
      mockJsonResponse(route, paged([{ id: "it1", name: "Medicare" }])));
    await page.route("**/api/v1/patient/patients/p1", (route) =>
      mockJsonResponse(route, { id: "p1", demographics: { firstName: "Maria", lastName: "Santos" } }));

    await page.route("**/api/v1/claims?*", (route) => mockJsonResponse(route, claimsPage));
    await page.route("**/api/v1/claims/c1", (route) => mockJsonResponse(route, claimDetail));

    let readyCalled = false;
    await page.route("**/api/v1/claims/c1/ready", (route) => {
      readyCalled = true;
      return mockJsonResponse(route, "c1");
    });

    await page.goto("/billing/claims");
    await expect(page.getByText("Claims")).toBeVisible();
    await expect(page.getByText("99213").or(page.getByRole("link", { name: /./ }))).toBeTruthy();

    // Open the claim, click Mark Ready
    await page.goto("/billing/claims/c1");
    await expect(page.getByText("Procedures (snapshot)")).toBeVisible();
    await page.getByRole("button", { name: /Mark Ready/ }).click();
    await expect.poll(() => readyCalled).toBe(true);
  });
});
```

> Adapt the auth/session bootstrap + any global route mocks from `tests/tickets/tickets-list.spec.ts` (patient/insurance-type list endpoints the page calls must also be mocked so name-resolution hooks resolve). Keep assertions resilient to the frontend-design polish.

- [ ] **Step 2: Run the spec**

Run: `cd clients/dashboard && npx playwright test tests/billing/claims-worklist.spec.ts`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/tests/billing/claims-worklist.spec.ts
git commit -m "test(dashboard): claims worklist e2e (route-mocked)"
```

---

## Task 13: Docs + changelog (golden rule #10)

**Files:**
- Modify (separate docs repo `github.com/fullstackhero/docs`): a Billing/Claims page under the module docs.
- Create: a changelog entry `src/content/docs/changelog/<date>-claims-module.md` (in the docs repo).

- [ ] **Step 1: Write a docs page** describing the Claims module: what a claim is, the `Draft→Ready→Submitted→Paid/Denied/Voided` lifecycle, that it's fed by `SuperBillSavedIntegrationEvent`, the freeze-past-Draft behavior, the `IClaimSubmitter` seam (stub today), the `Permissions.Claims.{View,Manage}`, and the dashboard **Billing** menu. Note real clearinghouse transport is a later slice.

- [ ] **Step 2: Add a changelog entry** summarizing the new Billing menu + Claims module (user-facing).

- [ ] **Step 3: Commit in the docs repo**

```bash
# in the docs repo working copy
git add src/content/docs/changelog src/content/docs/**/claims*
git commit -m "docs: claims (medical-claims billing) module + Billing menu"
```

- [ ] **Step 4: Final full verification (in clinic-app)**

Run:
```bash
dotnet build src/FSH.Starter.slnx
dotnet test src/Tests/Claims.Tests/Claims.Tests.csproj
dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj
cd clients/dashboard && npx tsc -b && npm run lint && npx playwright test tests/billing/claims-worklist.spec.ts
```
Expected: all green.

---

## Verification (whole feature)

- [ ] `dotnet build src/FSH.Starter.slnx` — 0 warnings (TreatWarningsAsErrors).
- [ ] `dotnet test src/Tests/Claims.Tests` — domain, event-handler (create/refresh/freeze/idempotent), transitions, query+summary, submitter, validators all green.
- [ ] `dotnet test src/Tests/Architecture.Tests` — validator pairing + module boundaries green (proves every new command/paginated-query handler has a validator and Claims references only `.Contracts`).
- [ ] `DbMigrator apply` runs `InitialClaims` on a dev DB; `claims` schema + `Claims`/`ClaimLines` tables + `ux_claims_superbill` present.
- [ ] Dashboard: `tsc -b` + `eslint` clean; Playwright worklist spec green.
- [ ] Manual: save a super bill in a patient report → a Draft claim appears in **Billing → Claims**; open it, **Mark Ready → Submit** (control number appears) → **Mark Paid**; re-saving the super bill after it left Draft does not change the claim (freeze).
- [ ] Docs repo updated + changelog entry added.
