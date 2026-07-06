# Procedures Performed (Super Bill) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port BackChart's per-report Procedures Performed (super bill) editor into clinic-solution-app: a `SuperBill` aggregate + GET/PUT slices in the Patient module, a `SuperBillSavedIntegrationEvent` seam for future pluggable billing providers, and one dashboard dialog reachable from both the report editor (with macro-text → Plan field) and a chart shortcut.

**Architecture:** Vertical-slice CQRS in `Modules.Patient` (Mediator handlers over EF Core — **no stored procedures**; the DB gets tables only). One new aggregate (`SuperBill` → `SuperBillProcedure` → `SuperBillProcedureDiagnostic`), replace-all save semantics, event published via `IEventBus` (the repo's cross-module pattern — `IOutboxStore` is Identity-owned). Frontend follows the existing patient-chart dialog patterns with TanStack Query.

**Tech Stack:** .NET 10, EF Core 10 (PostgreSQL), Mediator 3.x, FluentValidation 12, xUnit + Shouldly + NSubstitute (InMemory EF), React 19 + TanStack Query v5, Playwright (route-mocked).

**Spec:** `docs/superpowers/specs/2026-07-06-procedures-performed-design.md`

## Global Constraints

- **No stored procedures, DB functions, or triggers** — all logic is C# handlers; the migration creates tables/indexes only.
- Handlers `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)` on every await, propagate `CancellationToken` into every EF call.
- Every command handler has a `{Command}Validator` in the same feature folder (Architecture.Tests enforces).
- Do NOT modify `src/BuildingBlocks/**`.
- Backend style: file-scoped namespaces, 4-space indent, explicit types (`var` only when RHS-obvious), `is null`/`is not null`, records for DTOs/events. `TreatWarningsAsErrors` — warnings fail the build.
- Frontend: pass per-call data through `mutate(arg)` — never via state the mutation callbacks close over.
- Event type name `SuperBillSavedIntegrationEvent` and its namespace must stay stable once merged.
- Commit after every task (one commit per task — project convention).
- Backend test command: `dotnet test src/Tests/Patient.Tests --filter "FullyQualifiedName~<TestClass>"`. Full backend build: `dotnet build src/FSH.Starter.slnx`.

---

### Task 1: SuperBill domain aggregate

**Files:**
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/SuperBills/ReportProcedureItem.cs`
- Create: `src/Modules/Patient/Modules.Patient/Domain/SuperBill.cs`
- Create: `src/Modules/Patient/Modules.Patient/Domain/SuperBillProcedure.cs`
- Create: `src/Modules/Patient/Modules.Patient/Domain/SuperBillProcedureDiagnostic.cs`
- Test: `src/Tests/Patient.Tests/Domain/SuperBillTests.cs`

**Interfaces:**
- Consumes: `AggregateRoot<Guid>` (`FSH.Framework.Core.Domain`).
- Produces: `SuperBill.Create(Guid reportId, Guid patientId)`, `SuperBill.ReplaceProcedures(IReadOnlyList<ReportProcedureItem>)`, `SuperBillProcedure.Create(Guid superBillId, Guid procedureCodeId, string code, string? description, decimal charge, int displayOrder, IReadOnlyList<Guid> diagnosticIds)`, record `ReportProcedureItem(Guid ProcedureCodeId, string Code, string? Description, decimal Charge, IReadOnlyList<Guid> DiagnosticIds)`. Tasks 2–5 rely on these exact signatures.

- [ ] **Step 1: Write the failing domain test**

Create `src/Tests/Patient.Tests/Domain/SuperBillTests.cs`:

```csharp
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Domain;
using Shouldly;
using Xunit;

namespace Patient.Tests.Domain;

public sealed class SuperBillTests
{
    [Fact]
    public void Create_Should_Initialize_Unbilled_With_Empty_Procedures()
    {
        Guid reportId = Guid.NewGuid();
        Guid patientId = Guid.NewGuid();

        SuperBill bill = SuperBill.Create(reportId, patientId);

        bill.Id.ShouldNotBe(Guid.Empty);
        bill.ReportId.ShouldBe(reportId);
        bill.PatientId.ShouldBe(patientId);
        bill.IsBilled.ShouldBeFalse();
        bill.BilledDateUtc.ShouldBeNull();
        bill.Procedures.ShouldBeEmpty();
    }

    [Fact]
    public void ReplaceProcedures_Should_Replace_Set_And_Assign_DisplayOrder()
    {
        SuperBill bill = SuperBill.Create(Guid.NewGuid(), Guid.NewGuid());
        Guid dx1 = Guid.NewGuid();
        Guid dx2 = Guid.NewGuid();
        Guid pc1 = Guid.NewGuid();
        Guid pc2 = Guid.NewGuid();

        bill.ReplaceProcedures(
        [
            new ReportProcedureItem(pc1, "98940", "One to two spinal regions", 20.00m, [dx1, dx2]),
        ]);
        bill.ReplaceProcedures(
        [
            new ReportProcedureItem(pc2, "97110", "Therapeutic exercises", 35.50m, [dx1]),
            new ReportProcedureItem(pc2, "97110", "Therapeutic exercises", 35.50m, [dx2]),
        ]);

        bill.Procedures.Count.ShouldBe(2); // duplicates of the same code allowed (legacy)
        bill.Procedures[0].DisplayOrder.ShouldBe(0);
        bill.Procedures[1].DisplayOrder.ShouldBe(1);
        bill.Procedures[0].ProcedureCodeId.ShouldBe(pc2);
        bill.Procedures[0].Code.ShouldBe("97110");
        bill.Procedures[0].Charge.ShouldBe(35.50m);
        bill.Procedures[0].Diagnostics.Single().DiagnosticId.ShouldBe(dx1);
        bill.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void ProcedureCreate_Should_Clamp_Negative_Charge_And_Dedupe_Diagnostics()
    {
        Guid dx = Guid.NewGuid();

        SuperBillProcedure proc = SuperBillProcedure.Create(
            Guid.NewGuid(), Guid.NewGuid(), "98940", null, -5m, 0, [dx, dx]);

        proc.Charge.ShouldBe(0m);
        proc.Diagnostics.Count.ShouldBe(1);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Patient.Tests --filter "FullyQualifiedName~SuperBillTests"`
Expected: build FAILURE — `SuperBill`, `ReportProcedureItem` do not exist.

- [ ] **Step 3: Create the contract record**

Create `src/Modules/Patient/Modules.Patient.Contracts/v1/SuperBills/ReportProcedureItem.cs`:

```csharp
namespace FSH.Modules.Patient.Contracts.v1.SuperBills;

/// <summary>
/// One procedure line in a report's super bill: the procedure code (bare Administration id plus a
/// code/description snapshot taken at save time — the <c>PatientProblem.DiagnosticCode</c> pattern),
/// the charge, and the incident diagnostics that justify it (legacy <c>SuperBillProcedures</c> row group).
/// </summary>
public sealed record ReportProcedureItem(
    Guid ProcedureCodeId,
    string Code,
    string? Description,
    decimal Charge,
    IReadOnlyList<Guid> DiagnosticIds);
```

- [ ] **Step 4: Create the domain entities**

Create `src/Modules/Patient/Modules.Patient/Domain/SuperBillProcedureDiagnostic.cs`:

```csharp
namespace FSH.Modules.Patient.Domain;

/// <summary>Links a <see cref="SuperBillProcedure"/> to one incident diagnostic (legacy
/// <c>sbpDiagnosticsID</c>). Bare <see cref="DiagnosticId"/> into the Administration
/// custom-diagnostics catalog — no cross-module FK, mirroring <see cref="PatientIncidentDiagnostic"/>.</summary>
public sealed class SuperBillProcedureDiagnostic
{
    public Guid SuperBillProcedureId { get; private set; }
    public Guid DiagnosticId { get; private set; }

    private SuperBillProcedureDiagnostic() { }

    public static SuperBillProcedureDiagnostic Create(Guid superBillProcedureId, Guid diagnosticId) =>
        new() { SuperBillProcedureId = superBillProcedureId, DiagnosticId = diagnosticId };
}
```

Create `src/Modules/Patient/Modules.Patient/Domain/SuperBillProcedure.cs`:

```csharp
namespace FSH.Modules.Patient.Domain;

/// <summary>
/// One procedure administered in a report's super bill (legacy <c>SuperBillProcedures</c> —
/// <c>sbpProcedureCodeID</c>/<c>sbpCharge</c>). <see cref="Code"/>/<see cref="Description"/> are
/// snapshots taken at save time so saved rows render without a cross-module lookup and stay
/// billing-correct if the catalog changes. Duplicate procedure codes per bill are allowed (legacy).
/// </summary>
public sealed class SuperBillProcedure
{
    public Guid Id { get; private set; }
    public Guid SuperBillId { get; private set; }

    /// <summary>Bare id into the Administration <c>ProcedureCode</c> catalog — no cross-module FK.</summary>
    public Guid ProcedureCodeId { get; private set; }

    public string Code { get; private set; } = default!;
    public string? Description { get; private set; }
    public decimal Charge { get; private set; }
    public int DisplayOrder { get; private set; }

    public IReadOnlyList<SuperBillProcedureDiagnostic> Diagnostics => _diagnostics.AsReadOnly();
    private readonly List<SuperBillProcedureDiagnostic> _diagnostics = [];

    private SuperBillProcedure() { }

    public static SuperBillProcedure Create(
        Guid superBillId,
        Guid procedureCodeId,
        string code,
        string? description,
        decimal charge,
        int displayOrder,
        IReadOnlyList<Guid> diagnosticIds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(diagnosticIds);

        var procedure = new SuperBillProcedure
        {
            Id = Guid.CreateVersion7(),
            SuperBillId = superBillId,
            ProcedureCodeId = procedureCodeId,
            Code = code.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Charge = charge < 0 ? 0 : charge,
            DisplayOrder = displayOrder,
        };

        foreach (Guid diagnosticId in diagnosticIds.Distinct())
        {
            procedure._diagnostics.Add(SuperBillProcedureDiagnostic.Create(procedure.Id, diagnosticId));
        }

        return procedure;
    }
}
```

Create `src/Modules/Patient/Modules.Patient/Domain/SuperBill.cs`:

```csharp
using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Contracts.v1.SuperBills;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// The super bill of one report (legacy <c>SuperBills</c> — <c>sbReportID</c>/<c>sbBilled</c>/
/// <c>sbBilledDate</c>): the "Procedures Performed" set. One per report (unique <see cref="ReportId"/>).
/// Saving replaces the whole procedure set in one transaction — the C# equivalent of the legacy
/// delete-all-then-reinsert stored procs. <see cref="IsBilled"/>/<see cref="BilledDateUtc"/> stay
/// unset until a billing-provider module (subscribing to <c>SuperBillSavedIntegrationEvent</c>) lands.
/// </summary>
public sealed class SuperBill : AggregateRoot<Guid>
{
    public Guid ReportId { get; private set; }
    public Guid PatientId { get; private set; }
    public bool IsBilled { get; private set; }
    public DateTime? BilledDateUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public IReadOnlyList<SuperBillProcedure> Procedures => _procedures.AsReadOnly();
    private readonly List<SuperBillProcedure> _procedures = [];

    private SuperBill() { }

    public static SuperBill Create(Guid reportId, Guid patientId)
    {
        if (reportId == Guid.Empty)
        {
            throw new ArgumentException("Report id is required.", nameof(reportId));
        }

        if (patientId == Guid.Empty)
        {
            throw new ArgumentException("Patient id is required.", nameof(patientId));
        }

        return new SuperBill
        {
            Id = Guid.CreateVersion7(),
            ReportId = reportId,
            PatientId = patientId,
            IsBilled = false,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    /// <summary>Replaces the whole procedure set (legacy <c>SuperBillProcedures_Set_Multi</c> semantics).</summary>
    public void ReplaceProcedures(IReadOnlyList<ReportProcedureItem> procedures)
    {
        ArgumentNullException.ThrowIfNull(procedures);

        _procedures.Clear();
        for (int i = 0; i < procedures.Count; i++)
        {
            ReportProcedureItem item = procedures[i];
            _procedures.Add(SuperBillProcedure.Create(
                Id, item.ProcedureCodeId, item.Code, item.Description, item.Charge, i, item.DiagnosticIds));
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test src/Tests/Patient.Tests --filter "FullyQualifiedName~SuperBillTests"`
Expected: 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Patient/Modules.Patient.Contracts/v1/SuperBills/ReportProcedureItem.cs \
        src/Modules/Patient/Modules.Patient/Domain/SuperBill.cs \
        src/Modules/Patient/Modules.Patient/Domain/SuperBillProcedure.cs \
        src/Modules/Patient/Modules.Patient/Domain/SuperBillProcedureDiagnostic.cs \
        src/Tests/Patient.Tests/Domain/SuperBillTests.cs
git commit -m "feat(patient): SuperBill aggregate for report procedures performed

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: EF configuration, DbContext registration, migration

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Data/Configurations/SuperBillConfiguration.cs`
- Create: `src/Modules/Patient/Modules.Patient/Data/Configurations/SuperBillProcedureConfiguration.cs`
- Create: `src/Modules/Patient/Modules.Patient/Data/Configurations/SuperBillProcedureDiagnosticConfiguration.cs`
- Modify: `src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs` (add DbSet after line 65's `MedicationReconciledDates`, apply configs before `base.OnModelCreating`)
- Create (generated): `src/Host/FSH.Starter.Migrations.PostgreSQL/Patient/*_AddSuperBills.cs`

**Interfaces:**
- Consumes: Task 1 domain types.
- Produces: `PatientDbContext.SuperBills` (`DbSet<SuperBill>`); tables `patient.SuperBills`, `patient.SuperBillProcedures`, `patient.SuperBillProcedureDiagnostics`.

- [ ] **Step 1: Create the three EF configurations**

Create `src/Modules/Patient/Modules.Patient/Data/Configurations/SuperBillConfiguration.cs`:

```csharp
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class SuperBillConfiguration : IEntityTypeConfiguration<SuperBill>
{
    public void Configure(EntityTypeBuilder<SuperBill> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SuperBills");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.IsBilled).IsRequired();

        // One super bill per report (legacy sbReportID uniqueness).
        builder.HasIndex(x => x.ReportId).IsUnique();
        builder.HasIndex(x => x.PatientId);

        builder.HasMany(x => x.Procedures)
            .WithOne()
            .HasForeignKey(p => p.SuperBillId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
```

Create `src/Modules/Patient/Modules.Patient/Data/Configurations/SuperBillProcedureConfiguration.cs`:

```csharp
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class SuperBillProcedureConfiguration : IEntityTypeConfiguration<SuperBillProcedure>
{
    public void Configure(EntityTypeBuilder<SuperBillProcedure> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SuperBillProcedures");
        builder.HasKey(x => x.Id);

        // Reached only through SuperBill.Procedures — without this EF marks adds as Modified.
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SuperBillId).IsRequired();
        builder.Property(x => x.ProcedureCodeId).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.Charge).HasColumnType("numeric(10,2)").IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();

        builder.HasIndex(x => x.SuperBillId);

        builder.HasMany(x => x.Diagnostics)
            .WithOne()
            .HasForeignKey(d => d.SuperBillProcedureId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Create `src/Modules/Patient/Modules.Patient/Data/Configurations/SuperBillProcedureDiagnosticConfiguration.cs`:

```csharp
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class SuperBillProcedureDiagnosticConfiguration : IEntityTypeConfiguration<SuperBillProcedureDiagnostic>
{
    public void Configure(EntityTypeBuilder<SuperBillProcedureDiagnostic> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SuperBillProcedureDiagnostics");
        builder.HasKey(x => new { x.SuperBillProcedureId, x.DiagnosticId });

        builder.Property(x => x.SuperBillProcedureId).IsRequired();
        builder.Property(x => x.DiagnosticId).IsRequired();
    }
}
```

- [ ] **Step 2: Register in PatientDbContext**

In `src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs`, after the `MedicationReconciledDates` DbSet property add:

```csharp
    public DbSet<Domain.SuperBill> SuperBills => Set<Domain.SuperBill>();
```

In `OnModelCreating`, after `modelBuilder.ApplyConfiguration(new MedicationReconciledDateConfiguration());` and **before** `base.OnModelCreating(modelBuilder);` add:

```csharp
        modelBuilder.ApplyConfiguration(new SuperBillConfiguration());
        modelBuilder.ApplyConfiguration(new SuperBillProcedureConfiguration());
        modelBuilder.ApplyConfiguration(new SuperBillProcedureDiagnosticConfiguration());
```

- [ ] **Step 3: Build, then add the migration**

The snapshot must be current before `migrations add` (repo rule):

```bash
dotnet build src/FSH.Starter.slnx
dotnet tool restore
dotnet ef migrations add AddSuperBills \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context PatientDbContext \
  --output-dir Patient
```

Expected: a new `src/Host/FSH.Starter.Migrations.PostgreSQL/Patient/<timestamp>_AddSuperBills.cs` creating the three tables with the unique `ReportId` index, cascade FKs, `numeric(10,2)` Charge, and a `TenantId` column on `SuperBills` (added automatically by `BaseDbContext` tenant conventions — do NOT hand-edit it away).

- [ ] **Step 4: Verify the build and full Patient test suite still pass**

Run: `dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Patient.Tests`
Expected: build succeeds, all existing tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Patient/Modules.Patient/Data/ src/Host/FSH.Starter.Migrations.PostgreSQL/Patient/
git commit -m "feat(patient): SuperBill EF configuration + AddSuperBills migration

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Contracts — DTOs, query/command, integration event, permissions

**Files:**
- Create: `src/Modules/Patient/Modules.Patient.Contracts/Dtos/SuperBillDto.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/SuperBills/GetReportProceduresQuery.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/SuperBills/SetReportProceduresCommand.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/Events/SuperBillSavedIntegrationEvent.cs`
- Modify: `src/Modules/Patient/Modules.Patient.Contracts/Authorization/PatientPermissions.cs`

**Interfaces:**
- Consumes: `ReportProcedureItem` (Task 1), `IIntegrationEvent` (`FSH.Framework.Eventing.Abstractions`), `ICommand<Unit>`/`IQuery<T>` (Mediator).
- Produces: `SuperBillDto(Guid? Id, Guid ReportId, bool IsBilled, DateTime? BilledDateUtc, IReadOnlyList<SuperBillProcedureDto> Procedures)`; `SuperBillProcedureDto(Guid Id, Guid ProcedureCodeId, string Code, string? Description, decimal Charge, int DisplayOrder, IReadOnlyList<Guid> DiagnosticIds)`; `GetReportProceduresQuery(Guid ReportId) : IQuery<SuperBillDto>`; `SetReportProceduresCommand(Guid ReportId, IReadOnlyList<ReportProcedureItem> Procedures) : ICommand<Unit>`; `SuperBillSavedIntegrationEvent`; `PatientPermissions.SuperBills.View` = `"Permissions.Patient.SuperBills.View"`, `PatientPermissions.SuperBills.Manage` = `"Permissions.Patient.SuperBills.Manage"`.

- [ ] **Step 1: Create the DTOs**

Create `src/Modules/Patient/Modules.Patient.Contracts/Dtos/SuperBillDto.cs`:

```csharp
namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record SuperBillProcedureDto(
    Guid Id,
    Guid ProcedureCodeId,
    string Code,
    string? Description,
    decimal Charge,
    int DisplayOrder,
    IReadOnlyList<Guid> DiagnosticIds);

/// <summary>The procedures performed (super bill) of one report. <see cref="Id"/> is null when the
/// report has no super bill yet — the GET endpoint returns an empty shell instead of 404 so the
/// dialog can open on a fresh report (legacy <c>SuperBillProcedures_Get_XML</c> behavior).</summary>
public sealed record SuperBillDto(
    Guid? Id,
    Guid ReportId,
    bool IsBilled,
    DateTime? BilledDateUtc,
    IReadOnlyList<SuperBillProcedureDto> Procedures);
```

- [ ] **Step 2: Create the query and command**

Create `src/Modules/Patient/Modules.Patient.Contracts/v1/SuperBills/GetReportProceduresQuery.cs`:

```csharp
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.SuperBills;

/// <summary>Gets the procedures performed (super bill) for a report; empty shell when none exists yet.</summary>
public sealed record GetReportProceduresQuery(Guid ReportId) : IQuery<SuperBillDto>;
```

Create `src/Modules/Patient/Modules.Patient.Contracts/v1/SuperBills/SetReportProceduresCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.SuperBills;

/// <summary>Replaces the whole procedures-performed set of a report's super bill, creating the
/// super bill on first save (legacy <c>SuperBills_Insert</c> + <c>SuperBillProcedures_Set_Multi</c>).
/// An empty list is valid and clears the set.</summary>
public sealed record SetReportProceduresCommand(
    Guid ReportId,
    IReadOnlyList<ReportProcedureItem> Procedures) : ICommand<Unit>;
```

- [ ] **Step 3: Create the integration event**

Create `src/Modules/Patient/Modules.Patient.Contracts/Events/SuperBillSavedIntegrationEvent.cs`:

```csharp
using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Patient.Contracts.v1.SuperBills;

namespace FSH.Modules.Patient.Contracts.Events;

/// <summary>
/// Raised after a report's procedures-performed set is saved. This is the pluggable billing seam:
/// third-party billing-provider modules (Kareo/Cvikota-style, chosen per tenant) subscribe with
/// <c>IIntegrationEventHandler&lt;SuperBillSavedIntegrationEvent&gt;</c> — no changes to the Patient
/// module are needed to add a provider. Keep this type's name and namespace stable.
/// </summary>
public sealed record SuperBillSavedIntegrationEvent(
    Guid Id,
    DateTime OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid SuperBillId,
    Guid ReportId,
    Guid PatientId,
    bool IsBilled,
    IReadOnlyList<ReportProcedureItem> Procedures)
    : IIntegrationEvent;
```

Note: if `Modules.Patient.Contracts.csproj` does not already reference `FSH.Framework.Eventing.Abstractions`, add the project reference (check how `Modules.Billing.Contracts.csproj` references it and copy that `ProjectReference` line).

- [ ] **Step 4: Add permissions**

In `src/Modules/Patient/Modules.Patient.Contracts/Authorization/PatientPermissions.cs`, after the `Documents` class add:

```csharp
    public static class SuperBills
    {
        public const string Resource = "Patient.SuperBills";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Manage = $"Permissions.{Resource}.Manage";
    }
```

In the `All` list, after the `Documents` entries add:

```csharp
        new("View Procedures Performed",   ActionConstants.View, SuperBills.Resource, IsBasic: true),
        new("Manage Procedures Performed", "Manage",             SuperBills.Resource),
```

- [ ] **Step 5: Build**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: success (contracts compile; nothing consumes them yet).

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Patient/Modules.Patient.Contracts/
git commit -m "feat(patient): SuperBill contracts, SuperBillSavedIntegrationEvent, permissions

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: GET /reports/{id}/procedures slice

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/GetReportProcedures/GetReportProceduresQueryHandler.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/GetReportProcedures/GetReportProceduresEndpoint.cs`
- Modify: `src/Modules/Patient/Modules.Patient/PatientModule.cs` (using + `MapEndpoints`)
- Test: `src/Tests/Patient.Tests/Features/SuperBillHandlerTests.cs`

**Interfaces:**
- Consumes: `PatientDbContext.SuperBills` (Task 2), `GetReportProceduresQuery`/`SuperBillDto` (Task 3), `NotFoundException` (`FSH.Framework.Core.Exceptions`).
- Produces: route `GET api/v1/patient/reports/{id:guid}/procedures` gated on `PatientPermissions.SuperBills.View`; handler class `GetReportProceduresQueryHandler`. Task 5's tests reuse this test file's `CreateContext`/`SeedReport` helpers.

- [ ] **Step 1: Write the failing handler tests**

Create `src/Tests/Patient.Tests/Features/SuperBillHandlerTests.cs` (the `CreateContext` helper is the repo-standard InMemory harness from `PatientNoteHandlerTests`; `SeedReport` inserts the parent report the slices resolve):

```csharp
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class SuperBillHandlerTests
{
    internal static PatientDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));

        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });

        return new PatientDbContext(
            accessor, options, settings, Substitute.For<IHostEnvironment>(), Substitute.For<IPhiEncryptor>());
    }

    internal static async Task<PatientReport> SeedReport(PatientDbContext db)
    {
        PatientReport report = PatientReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow.Date, null, null, false);
        db.PatientReports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    [Fact]
    public async Task Get_Should_Return_Empty_Shell_When_No_SuperBill_Exists()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        var sut = new GetReportProceduresQueryHandler(db);

        SuperBillDto dto = await sut.Handle(new GetReportProceduresQuery(report.Id), CancellationToken.None);

        dto.Id.ShouldBeNull();
        dto.ReportId.ShouldBe(report.Id);
        dto.IsBilled.ShouldBeFalse();
        dto.Procedures.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_Should_Throw_NotFound_For_Unknown_Report()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new GetReportProceduresQueryHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new GetReportProceduresQuery(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Get_Should_Return_Procedures_Ordered_By_DisplayOrder()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        SuperBill bill = SuperBill.Create(report.Id, report.PatientId);
        bill.ReplaceProcedures(
        [
            new ReportProcedureItem(Guid.NewGuid(), "98940", "One to two spinal regions", 20m, [dx]),
            new ReportProcedureItem(Guid.NewGuid(), "97110", "Therapeutic exercises", 35.5m, [dx]),
        ]);
        db.SuperBills.Add(bill);
        await db.SaveChangesAsync();
        var sut = new GetReportProceduresQueryHandler(db);

        SuperBillDto dto = await sut.Handle(new GetReportProceduresQuery(report.Id), CancellationToken.None);

        dto.Id.ShouldBe(bill.Id);
        dto.Procedures.Count.ShouldBe(2);
        dto.Procedures[0].Code.ShouldBe("98940");
        dto.Procedures[1].Code.ShouldBe("97110");
        dto.Procedures[0].DiagnosticIds.Single().ShouldBe(dx);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test src/Tests/Patient.Tests --filter "FullyQualifiedName~SuperBillHandlerTests"`
Expected: build FAILURE — `GetReportProceduresQueryHandler` does not exist.

- [ ] **Step 3: Implement the handler**

Create `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/GetReportProcedures/GetReportProceduresQueryHandler.cs`:

```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;

public sealed class GetReportProceduresQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetReportProceduresQuery, SuperBillDto>
{
    public async ValueTask<SuperBillDto> Handle(GetReportProceduresQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        bool reportExists = await dbContext.PatientReports
            .AnyAsync(x => x.Id == query.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
        if (!reportExists)
        {
            throw new NotFoundException($"Report {query.ReportId} not found.");
        }

        SuperBill? bill = await dbContext.SuperBills
            .AsNoTracking()
            .Include(x => x.Procedures)
            .ThenInclude(p => p.Diagnostics)
            .FirstOrDefaultAsync(x => x.ReportId == query.ReportId, cancellationToken)
            .ConfigureAwait(false);

        if (bill is null)
        {
            return new SuperBillDto(null, query.ReportId, false, null, []);
        }

        List<SuperBillProcedureDto> procedures = [.. bill.Procedures
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new SuperBillProcedureDto(
                p.Id,
                p.ProcedureCodeId,
                p.Code,
                p.Description,
                p.Charge,
                p.DisplayOrder,
                [.. p.Diagnostics.Select(d => d.DiagnosticId)]))];

        return new SuperBillDto(bill.Id, bill.ReportId, bill.IsBilled, bill.BilledDateUtc, procedures);
    }
}
```

- [ ] **Step 4: Implement the endpoint**

Create `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/GetReportProcedures/GetReportProceduresEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;

public static class GetReportProceduresEndpoint
{
    internal static RouteHandlerBuilder MapGetReportProceduresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/reports/{id:guid}/procedures",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetReportProceduresQuery(id), ct)))
            .WithName("GetReportProcedures")
            .WithSummary("Get the procedures performed (super bill) for a report")
            .RequirePermission(PatientPermissions.SuperBills.View)
            .Produces<SuperBillDto>();
    }
}
```

- [ ] **Step 5: Wire in PatientModule**

In `src/Modules/Patient/Modules.Patient/PatientModule.cs`:
- Add using (alphabetical among the `Features.v1` usings): `using FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;`
- In `MapEndpoints`, in the Report endpoints block after `group.MapSetReportProblemsEndpoint();` add:

```csharp
        group.MapGetReportProceduresEndpoint();
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test src/Tests/Patient.Tests --filter "FullyQualifiedName~SuperBillHandlerTests"`
Expected: 3 tests PASS.

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/ \
        src/Modules/Patient/Modules.Patient/PatientModule.cs \
        src/Tests/Patient.Tests/Features/SuperBillHandlerTests.cs
git commit -m "feat(patient): GET /reports/{id}/procedures — super bill read slice

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: PUT /reports/{id}/procedures slice (replace-all + event)

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/SetReportProcedures/SetReportProceduresCommandHandler.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/SetReportProcedures/SetReportProceduresCommandValidator.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/SetReportProcedures/SetReportProceduresEndpoint.cs`
- Modify: `src/Modules/Patient/Modules.Patient/PatientModule.cs` (using + `MapEndpoints`)
- Test: `src/Tests/Patient.Tests/Features/SuperBillHandlerTests.cs` (append), `src/Tests/Patient.Tests/Validators/SetReportProceduresCommandValidatorTests.cs`

**Interfaces:**
- Consumes: Tasks 1–4 types; `IEventBus` (`FSH.Framework.Eventing.Abstractions`), `IMultiTenantContextAccessor<AppTenantInfo>` (`Finbuckle.MultiTenant.Abstractions` + `FSH.Framework.Shared.Multitenancy`), `TimeProvider`.
- Produces: route `PUT api/v1/patient/reports/{id:guid}/procedures` gated on `PatientPermissions.SuperBills.Manage`, returning 204; publishes `SuperBillSavedIntegrationEvent` with `Source = "Patient"`.

- [ ] **Step 1: Write the failing tests**

Append to `src/Tests/Patient.Tests/Features/SuperBillHandlerTests.cs` (inside the class; add usings `FSH.Framework.Eventing.Abstractions;`, `FSH.Modules.Patient.Contracts.Events;`, `FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;` to the file header):

```csharp
    private static SetReportProceduresCommandHandler CreateSetHandler(PatientDbContext db, IEventBus? bus = null)
    {
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));
        return new SetReportProceduresCommandHandler(
            db, bus ?? Substitute.For<IEventBus>(), accessor, TimeProvider.System);
    }

    [Fact]
    public async Task Set_Should_Create_SuperBill_On_First_Save_And_Publish_Event()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        IEventBus bus = Substitute.For<IEventBus>();
        Guid dx = Guid.NewGuid();
        var sut = CreateSetHandler(db, bus);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
        [
            new ReportProcedureItem(Guid.NewGuid(), "98940", "One to two spinal regions", 20m, [dx]),
        ]), CancellationToken.None);

        SuperBill saved = await db.SuperBills.Include(x => x.Procedures).ThenInclude(p => p.Diagnostics)
            .SingleAsync(x => x.ReportId == report.Id);
        saved.PatientId.ShouldBe(report.PatientId);
        saved.Procedures.Single().Code.ShouldBe("98940");
        saved.Procedures.Single().Diagnostics.Single().DiagnosticId.ShouldBe(dx);
        await bus.Received(1).PublishAsync(
            Arg.Is<SuperBillSavedIntegrationEvent>(e =>
                e.ReportId == report.Id && e.Source == "Patient" && e.Procedures.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_Should_Replace_Existing_Set()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        var sut = CreateSetHandler(db);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
        [
            new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx]),
            new ReportProcedureItem(Guid.NewGuid(), "97110", null, 35m, [dx]),
        ]), CancellationToken.None);
        await sut.Handle(new SetReportProceduresCommand(report.Id,
        [
            new ReportProcedureItem(Guid.NewGuid(), "97140", null, 40m, [dx]),
        ]), CancellationToken.None);

        SuperBill saved = await db.SuperBills.Include(x => x.Procedures)
            .SingleAsync(x => x.ReportId == report.Id);
        saved.Procedures.Single().Code.ShouldBe("97140");
        db.SuperBills.Count().ShouldBe(1); // still one super bill per report
    }

    [Fact]
    public async Task Set_Should_Allow_Empty_List_To_Clear()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        var sut = CreateSetHandler(db);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
            [new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx])]), CancellationToken.None);
        await sut.Handle(new SetReportProceduresCommand(report.Id, []), CancellationToken.None);

        SuperBill saved = await db.SuperBills.Include(x => x.Procedures)
            .SingleAsync(x => x.ReportId == report.Id);
        saved.Procedures.ShouldBeEmpty();
    }

    [Fact]
    public async Task Set_Should_Throw_NotFound_For_Unknown_Report()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = CreateSetHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new SetReportProceduresCommand(Guid.NewGuid(), []), CancellationToken.None).AsTask());
    }
```

Create `src/Tests/Patient.Tests/Validators/SetReportProceduresCommandValidatorTests.cs`:

```csharp
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;
using Shouldly;
using Xunit;

namespace Patient.Tests.Validators;

public sealed class SetReportProceduresCommandValidatorTests
{
    private readonly SetReportProceduresCommandValidator _sut = new();

    private static ReportProcedureItem Item(
        decimal charge = 20m, string code = "98940", Guid? dx = null, Guid? procedureCodeId = null) =>
        new(procedureCodeId ?? Guid.NewGuid(), code, null, charge, [dx ?? Guid.NewGuid()]);

    [Fact]
    public void Valid_Command_Should_Pass() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item()])).IsValid.ShouldBeTrue();

    [Fact]
    public void Empty_Procedures_List_Should_Pass() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [])).IsValid.ShouldBeTrue();

    [Fact]
    public void Empty_ReportId_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.Empty, [])).IsValid.ShouldBeFalse();

    [Fact]
    public void Negative_Charge_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item(charge: -1m)]))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void Blank_Code_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item(code: " ")]))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void Empty_ProcedureCodeId_Should_Fail() =>
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [Item(procedureCodeId: Guid.Empty)]))
            .IsValid.ShouldBeFalse();

    [Fact]
    public void Procedure_Without_Diagnostics_Should_Fail()
    {
        var item = new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, []);
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [item])).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Procedure_With_Empty_Diagnostic_Guid_Should_Fail()
    {
        var item = new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [Guid.Empty]);
        _sut.Validate(new SetReportProceduresCommand(Guid.NewGuid(), [item])).IsValid.ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test src/Tests/Patient.Tests --filter "FullyQualifiedName~SuperBill"`
Expected: build FAILURE — `SetReportProceduresCommandHandler` / `SetReportProceduresCommandValidator` do not exist.

- [ ] **Step 3: Implement the validator**

Create `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/SetReportProcedures/SetReportProceduresCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.SuperBills;

namespace FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;

public sealed class SetReportProceduresCommandValidator : AbstractValidator<SetReportProceduresCommand>
{
    public SetReportProceduresCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Procedures).NotNull();
        RuleForEach(x => x.Procedures).ChildRules(procedure =>
        {
            procedure.RuleFor(p => p.ProcedureCodeId).NotEmpty();
            procedure.RuleFor(p => p.Code).NotEmpty().MaximumLength(32);
            procedure.RuleFor(p => p.Description).MaximumLength(512);
            procedure.RuleFor(p => p.Charge).GreaterThanOrEqualTo(0);
            procedure.RuleFor(p => p.DiagnosticIds)
                .NotEmpty()
                .WithMessage("Each procedure must be linked to at least one diagnostic.");
            procedure.RuleForEach(p => p.DiagnosticIds).NotEmpty();
        });
    }
}
```

- [ ] **Step 4: Implement the handler**

Create `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/SetReportProcedures/SetReportProceduresCommandHandler.cs`:

```csharp
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Patient.Contracts.Events;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;

public sealed class SetReportProceduresCommandHandler(
    PatientDbContext dbContext,
    IEventBus eventBus,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    TimeProvider timeProvider)
    : ICommandHandler<SetReportProceduresCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetReportProceduresCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        SuperBill? bill = await dbContext.SuperBills
            .Include(x => x.Procedures)
            .ThenInclude(p => p.Diagnostics)
            .FirstOrDefaultAsync(x => x.ReportId == command.ReportId, cancellationToken)
            .ConfigureAwait(false);

        if (bill is null)
        {
            bill = SuperBill.Create(command.ReportId, report.PatientId);
            dbContext.SuperBills.Add(bill);
        }

        bill.ReplaceProcedures(command.Procedures ?? []);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Pluggable billing seam: provider modules (chosen per tenant) subscribe to this event —
        // published via IEventBus like Billing/Chat/Files (IOutboxStore is Identity-owned today).
        await eventBus.PublishAsync(new SuperBillSavedIntegrationEvent(
                Id: Guid.NewGuid(),
                OccurredOnUtc: timeProvider.GetUtcNow().UtcDateTime,
                TenantId: tenantAccessor.MultiTenantContext.TenantInfo?.Id,
                CorrelationId: Guid.NewGuid().ToString(),
                Source: "Patient",
                SuperBillId: bill.Id,
                ReportId: bill.ReportId,
                PatientId: bill.PatientId,
                IsBilled: bill.IsBilled,
                Procedures: command.Procedures ?? []), cancellationToken)
            .ConfigureAwait(false);

        return Unit.Value;
    }
}
```

Note: if `IEventBus.PublishAsync`'s actual signature differs (check `src/BuildingBlocks/Eventing.Abstractions/IEventBus.cs`), match it — Billing's call site (`BillingService.cs:250`) is the reference.

- [ ] **Step 5: Implement the endpoint and wire it**

Create `src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/SetReportProcedures/SetReportProceduresEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;

public static class SetReportProceduresEndpoint
{
    internal static RouteHandlerBuilder MapSetReportProceduresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}/procedures",
                async (Guid id, SetReportProceduresCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetReportProceduresCommand command = body with { ReportId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetReportProcedures")
            .WithSummary("Replace the procedures performed (super bill) for a report")
            .RequirePermission(PatientPermissions.SuperBills.Manage);
    }
}
```

In `src/Modules/Patient/Modules.Patient/PatientModule.cs`:
- Add using: `using FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;`
- After `group.MapGetReportProceduresEndpoint();` add:

```csharp
        group.MapSetReportProceduresEndpoint();
```

- [ ] **Step 6: Run all Patient tests to verify they pass**

Run: `dotnet test src/Tests/Patient.Tests`
Expected: all PASS (including Task 1/4 tests). Also run the architecture gate: `dotnet test src/Tests/Architecture.Tests` — the new handler/validator pairing must pass.

- [ ] **Step 7: Commit**

```bash
git add src/Modules/Patient/Modules.Patient/Features/v1/SuperBills/ \
        src/Modules/Patient/Modules.Patient/PatientModule.cs \
        src/Tests/Patient.Tests/
git commit -m "feat(patient): PUT /reports/{id}/procedures — replace-all save + SuperBillSavedIntegrationEvent

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Administration — `ids` filter on ListCustomDiagnostics

**Files:**
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/v1/CustomDiagnostics/ListCustomDiagnosticsQuery.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/CustomDiagnostics/ListCustomDiagnostics/ListCustomDiagnosticsQueryHandler.cs`
- Modify: `src/Modules/Administration/Modules.Administration/Features/v1/CustomDiagnostics/ListCustomDiagnostics/ListCustomDiagnosticsEndpoint.cs`
- Test: `src/Tests/Administration.Tests/` — add `Features/ListCustomDiagnosticsIdsFilterTests.cs` (mirror the harness of an existing Administration handler test file; find one with `ls src/Tests/Administration.Tests/Features` and copy its `CreateContext`)

**Interfaces:**
- Consumes: existing `ListCustomDiagnosticsQuery` slice.
- Produces: `ListCustomDiagnosticsQuery` gains `IReadOnlyList<Guid>? Ids = null` (last parameter); `GET /api/v1/administration/custom-diagnostics?ids=<guid>&ids=<guid>` filters to those ids. Task 8's dialog relies on this to resolve incident dx codes.

- [ ] **Step 1: Write the failing test**

Create `src/Tests/Administration.Tests/Features/ListCustomDiagnosticsIdsFilterTests.cs`. Copy the `CreateContext` helper style from an existing test in `src/Tests/Administration.Tests/Features/` (InMemory `AdministrationDbContext` with the substituted tenant accessor — same shape as `SuperBillHandlerTests.CreateContext` but for `AdministrationDbContext`, which needs no `IPhiEncryptor`). Test body:

```csharp
    [Fact]
    public async Task List_Should_Filter_By_Ids_When_Provided()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var keep = CustomDiagnostic.Create("M99.01", "Segmental dysfunction, cervical", null, true);
        var skip = CustomDiagnostic.Create("M54.5", "Low back pain", null, true);
        db.CustomDiagnostics.AddRange(keep, skip);
        await db.SaveChangesAsync();
        var sut = new ListCustomDiagnosticsQueryHandler(db);

        var result = await sut.Handle(
            new ListCustomDiagnosticsQuery(Ids: [keep.Id]), CancellationToken.None);

        result.Items.Single().Id.ShouldBe(keep.Id);
    }
```

(Adjust the `CustomDiagnostic.Create` argument list to the real factory signature in `src/Modules/Administration/Modules.Administration/Domain/CustomDiagnostic.cs` — read it first.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test src/Tests/Administration.Tests --filter "FullyQualifiedName~ListCustomDiagnosticsIdsFilterTests"`
Expected: build FAILURE — `Ids` parameter does not exist.

- [ ] **Step 3: Add the parameter and filter**

In `ListCustomDiagnosticsQuery.cs`, add a final parameter so the record becomes:

```csharp
public sealed record ListCustomDiagnosticsQuery(
    string? Search = null,
    bool? IsActive = null,
    bool? IsChiropractic = null,
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDir = null,
    IReadOnlyList<Guid>? Ids = null) : IQuery<PagedResponse<CustomDiagnosticDto>>;
```

In `ListCustomDiagnosticsQueryHandler.cs`, after the `IsChiropractic` filter block add:

```csharp
        if (query.Ids is { Count: > 0 })
        {
            q = q.Where(c => query.Ids.Contains(c.Id));
        }
```

In `ListCustomDiagnosticsEndpoint.cs`, add a `Guid[]? ids` lambda parameter (minimal APIs bind repeated `?ids=` query values to arrays) and pass it through:

```csharp
        return endpoints.MapGet("/custom-diagnostics",
                async (
                    string? search,
                    bool? isActive,
                    bool? isChiropractic,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    Guid[]? ids,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListCustomDiagnosticsQuery(
                            search, isActive, isChiropractic, pageNumber ?? 1, pageSize ?? 20,
                            sortBy, sortDir, ids is { Length: > 0 } ? ids : null), ct)))
```

(keep the existing `.WithName/.WithSummary/.RequirePermission` chain unchanged).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test src/Tests/Administration.Tests --filter "FullyQualifiedName~ListCustomDiagnostics"`
Expected: PASS (new test + any existing list tests).

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Administration/ src/Tests/Administration.Tests/
git commit -m "feat(administration): ids filter on custom-diagnostics list for dx resolution

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Dashboard API module + permission constants

**Files:**
- Create: `clients/dashboard/src/api/report-procedures.ts`
- Modify: `clients/dashboard/src/lib/patient-permissions.ts` (append after `DOCUMENT_PERMISSIONS`)
- Modify: `clients/dashboard/src/api/administration.ts` (add `ids` to `ListCustomDiagnosticsParams` + `listCustomDiagnostics`)

**Interfaces:**
- Consumes: `apiFetch` (`@/lib/api-client`), backend routes from Tasks 4–6.
- Produces: `getReportProcedures(reportId): Promise<SuperBillDto>`, `setReportProcedures(input: SetReportProceduresInput): Promise<void>`, types `SuperBillDto`, `SuperBillProcedureDto`, `ReportProcedureInput`; `SUPERBILL_PERMISSIONS.view/manage`; `listCustomDiagnostics({ ids })`. Tasks 8–10 import these names exactly.

- [ ] **Step 1: Create the API module**

Create `clients/dashboard/src/api/report-procedures.ts`:

```typescript
import { apiFetch } from "@/lib/api-client";

export type SuperBillProcedureDto = {
  id: string;
  procedureCodeId: string;
  code: string;
  description?: string | null;
  charge: number;
  displayOrder: number;
  diagnosticIds: string[];
};

export type SuperBillDto = {
  /** null until the report's first Procedures Performed save. */
  id?: string | null;
  reportId: string;
  isBilled: boolean;
  billedDateUtc?: string | null;
  procedures: SuperBillProcedureDto[];
};

export type ReportProcedureInput = {
  procedureCodeId: string;
  code: string;
  description?: string | null;
  charge: number;
  diagnosticIds: string[];
};

export type SetReportProceduresInput = {
  reportId: string;
  procedures: ReportProcedureInput[];
};

export function getReportProcedures(reportId: string): Promise<SuperBillDto> {
  return apiFetch<SuperBillDto>(
    `/api/v1/patient/reports/${encodeURIComponent(reportId)}/procedures`,
  );
}

export async function setReportProcedures(input: SetReportProceduresInput): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/reports/${encodeURIComponent(input.reportId)}/procedures`,
    {
      method: "PUT",
      body: JSON.stringify({ reportId: input.reportId, procedures: input.procedures }),
    },
  );
}
```

- [ ] **Step 2: Add permission constants**

In `clients/dashboard/src/lib/patient-permissions.ts`, append after the `DocumentPermissionKey` export:

```typescript
export const SUPERBILL_PERMISSIONS = {
  view:   "Permissions.Patient.SuperBills.View",
  manage: "Permissions.Patient.SuperBills.Manage",
} as const;

export type SuperBillPermissionKey = keyof typeof SUPERBILL_PERMISSIONS;
```

- [ ] **Step 3: Add the `ids` param to listCustomDiagnostics**

In `clients/dashboard/src/api/administration.ts`, add to `ListCustomDiagnosticsParams`:

```typescript
  /** Resolve specific diagnostics by id (repeated ?ids= query params). */
  ids?: string[];
```

and in `listCustomDiagnostics`, after the `isChiropractic` handling add:

```typescript
  for (const id of params.ids ?? []) query.append("ids", id);
```

- [ ] **Step 4: Verify the frontend compiles**

Run (from `clients/dashboard`): `npm run build`
Expected: type-check + build succeed.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/api/report-procedures.ts \
        clients/dashboard/src/lib/patient-permissions.ts \
        clients/dashboard/src/api/administration.ts
git commit -m "feat(dashboard): report-procedures API module + SuperBill permissions

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 8: Procedures Performed dialog component

**Files:**
- Create: `clients/dashboard/src/pages/patient-charts/procedures-performed-dialog.tsx`

**Interfaces:**
- Consumes: Task 7 API + permissions; `getPatientIncident` (`@/api/incidents`), `searchPatientReports` (`@/api/reports`), `listCustomDiagnostics`, `listProcedureCodes`, `listInsuranceTypeProcedures`, `useInsuranceTypeOptions`, `useProcedureCategoryOptions` (`@/api/administration`), `IncidentDialog` (`@/pages/patient-charts/incident-dialog` — check its exported name/props with Grep before use; it edits an incident's dx set), UI kit (`Dialog*`, `Button`, `Input`, `Combobox`, `EntityStatusBadge`), `useAuth`, `toast`, `describe`/`formatDate`.
- Produces: `export function ProceduresPerformedDialog(props: { incidentId: string; reportId: string | null; open: boolean; onClose(): void; onMacroText?: (text: string) => void })`. When `reportId` is null it shows a report-picker phase first (Export Reports pattern). Tasks 9–10 mount it with exactly these props.

- [ ] **Step 1: Check the incident edit dialog's export before writing**

Run: `Grep "export function" clients/dashboard/src/pages/patient-charts/incident-dialog.tsx` and note the component name and props (it needs `incidentId`, `open`/`onClose`-style props; adapt the "Edit Dx Codes" wiring below to the real signature).

- [ ] **Step 2: Create the dialog**

Create `clients/dashboard/src/pages/patient-charts/procedures-performed-dialog.tsx`. Complete implementation (adjust only the `IncidentDialog` import/props to what Step 1 found):

```tsx
import { useEffect, useMemo, useRef, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ClipboardList, Pencil, Plus, Save, X } from "lucide-react";
import { toast } from "sonner";
import { getPatientIncident } from "@/api/incidents";
import { searchPatientReports } from "@/api/reports";
import {
  listCustomDiagnostics,
  listInsuranceTypeProcedures,
  listProcedureCodes,
  useInsuranceTypeOptions,
  useProcedureCategoryOptions,
} from "@/api/administration";
import {
  getReportProcedures,
  setReportProcedures,
  type ReportProcedureInput,
} from "@/api/report-procedures";
import { SUPERBILL_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field } from "@/components/list";
import { EntityStatusBadge } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { IncidentDialog } from "@/pages/patient-charts/incident-dialog";

type Props = {
  incidentId: string;
  /** When null (chart-shortcut context) the dialog shows a report-picker phase first. */
  reportId: string | null;
  open: boolean;
  onClose(): void;
  /** Report-editor context only: receives a picked procedure's macro text for the Plan field. */
  onMacroText?: (text: string) => void;
};

/** One editable procedure row. `charge` stays a string while editing; parsed on save. */
type ProcRow = {
  key: number;
  procedureCodeId: string;
  code: string;
  description: string | null;
  charge: string;
  dxIds: string[];
};

export function ProceduresPerformedDialog({
  incidentId,
  reportId,
  open,
  onClose,
  onMacroText,
}: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const canManage = user?.permissions?.includes(SUPERBILL_PERMISSIONS.manage) ?? false;

  // Two-phase: chart context picks a report first; report-editor context skips the picker.
  const [pickedReportId, setPickedReportId] = useState<string | null>(reportId);
  const activeReportId = reportId ?? pickedReportId;

  const [rows, setRows] = useState<ProcRow[]>([]);
  const [hydratedFor, setHydratedFor] = useState<string | null>(null);
  const [addMacroText, setAddMacroText] = useState(true);
  const [insuranceTypeId, setInsuranceTypeId] = useState<string | null>(null);
  const [categoryId, setCategoryId] = useState<string | null>(null);
  const [editDxOpen, setEditDxOpen] = useState(false);
  const rowKey = useRef(0);

  const reset = () => {
    setPickedReportId(reportId);
    setRows([]);
    setHydratedFor(null);
    setEditDxOpen(false);
  };

  // ── Report picker phase (chart-shortcut context) ──
  const reportsQuery = useQuery({
    queryKey: ["reports", incidentId],
    queryFn: () => searchPatientReports({ incidentId, pageSize: 100 }),
    enabled: open && reportId === null && pickedReportId === null,
    placeholderData: keepPreviousData,
  });

  // ── Incident dx codes (the checkbox axis) ──
  const incidentQuery = useQuery({
    queryKey: ["incident", incidentId],
    queryFn: () => getPatientIncident(incidentId),
    enabled: open,
  });
  const dxIds = useMemo(() => incidentQuery.data?.diagnosticIds ?? [], [incidentQuery.data]);

  const dxDetailsQuery = useQuery({
    queryKey: ["custom-diagnostics", "by-ids", [...dxIds].sort().join(",")],
    queryFn: () => listCustomDiagnostics({ ids: dxIds, pageSize: 200 }),
    enabled: open && dxIds.length > 0,
  });
  const dxLabel = useMemo(() => {
    const map = new Map<string, { code: string; description: string | null }>();
    for (const d of dxDetailsQuery.data?.items ?? []) {
      map.set(d.id, { code: d.code, description: d.description ?? null });
    }
    return map;
  }, [dxDetailsQuery.data]);

  // ── Existing super bill (hydrates rows once per report) ──
  const proceduresQuery = useQuery({
    queryKey: ["report-procedures", activeReportId],
    queryFn: () => getReportProcedures(activeReportId!),
    enabled: open && !!activeReportId,
  });

  useEffect(() => {
    if (!open || !activeReportId || !proceduresQuery.data) return;
    if (hydratedFor === activeReportId) return;
    setRows(
      proceduresQuery.data.procedures.map((p) => ({
        key: rowKey.current++,
        procedureCodeId: p.procedureCodeId,
        code: p.code,
        description: p.description ?? null,
        charge: String(p.charge),
        dxIds: p.diagnosticIds,
      })),
    );
    setHydratedFor(activeReportId);
  }, [open, activeReportId, proceduresQuery.data, hydratedFor]);

  // ── Picker: insurance type + category → codes with negotiated prices ──
  const insuranceOptions = useInsuranceTypeOptions();
  const categoryOptions = useProcedureCategoryOptions();

  // Default to the first active insurance type (legacy incident/patient insurance-type
  // links are not ported — see the design spec).
  useEffect(() => {
    if (insuranceTypeId === null && insuranceOptions && insuranceOptions.length > 0) {
      setInsuranceTypeId(insuranceOptions[0].value);
    }
  }, [insuranceOptions, insuranceTypeId]);

  const codesQuery = useQuery({
    queryKey: ["procedure-codes", "picker", categoryId ?? "all"],
    queryFn: () =>
      listProcedureCodes({
        procedureCategoryId: categoryId ?? undefined,
        isActive: true,
        pageSize: 200,
        sortBy: "code",
      }),
    enabled: open && !!activeReportId,
    placeholderData: keepPreviousData,
  });

  const pricesQuery = useQuery({
    queryKey: ["insurance-type-procedures", insuranceTypeId],
    queryFn: () => listInsuranceTypeProcedures(insuranceTypeId!),
    enabled: open && !!insuranceTypeId,
  });
  const priceByCodeId = useMemo(() => {
    const map = new Map<string, number>();
    for (const p of pricesQuery.data ?? []) map.set(p.procedureCodeId, p.price);
    return map;
  }, [pricesQuery.data]);

  // ── Row operations ──
  const addProcedure = (codeId: string) => {
    const picked = codesQuery.data?.items.find((c) => c.id === codeId);
    if (!picked) return;
    if (dxIds.length === 0) {
      toast.warning("Please add at least one DX code before picking procedures.");
      return;
    }
    const price = priceByCodeId.get(picked.id);
    setRows((prev) => [
      ...prev,
      {
        key: rowKey.current++,
        procedureCodeId: picked.id,
        code: picked.code,
        description: picked.description ?? picked.name ?? null,
        charge: String(price ?? 0),
        dxIds: [...dxIds], // legacy default: the full incident dx set, checked
      },
    ]);
    if (addMacroText && onMacroText && picked.macroText) {
      onMacroText(picked.macroText);
    }
  };

  const removeRow = (key: number) => setRows((prev) => prev.filter((r) => r.key !== key));

  const toggleDx = (key: number, dxId: string) =>
    setRows((prev) =>
      prev.map((r) =>
        r.key === key
          ? {
              ...r,
              dxIds: r.dxIds.includes(dxId)
                ? r.dxIds.filter((x) => x !== dxId)
                : [...r.dxIds, dxId],
            }
          : r,
      ),
    );

  const setCharge = (key: number, charge: string) =>
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, charge } : r)));

  // ── Save (replace-all) ──
  const saveMutation = useMutation({
    mutationFn: setReportProcedures,
    onSuccess: (_data, variables) => {
      toast.success("Procedures saved.");
      void queryClient.invalidateQueries({ queryKey: ["report-procedures", variables.reportId] });
      setHydratedFor(null);
      onClose();
    },
    onError: (err) => toast.error("Failed to save procedures.", { description: describe(err) }),
  });

  const onSave = () => {
    if (!activeReportId) return;
    const unlinked = rows.filter((r) => r.dxIds.length === 0);
    if (unlinked.length > 0) {
      toast.warning("Every procedure must have at least one DX code checked.");
      return;
    }
    const procedures: ReportProcedureInput[] = rows.map((r) => ({
      procedureCodeId: r.procedureCodeId,
      code: r.code,
      description: r.description,
      charge: Number(r.charge) >= 0 && Number.isFinite(Number(r.charge)) ? Number(r.charge) : 0,
      diagnosticIds: r.dxIds,
    }));
    saveMutation.mutate({ reportId: activeReportId, procedures });
  };

  const showPicker = reportId === null && pickedReportId === null;

  return (
    <>
      <Dialog
        open={open}
        onOpenChange={(o) => {
          if (!o) {
            reset();
            onClose();
          }
        }}
      >
        <DialogContent className="!max-w-4xl">
          <DialogHeader>
            <DialogTitle>Procedures Performed</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            {showPicker ? (
              /* ── Phase 1 (chart context): pick the report ── */
              reportsQuery.isLoading ? (
                <div className="skeleton h-20 rounded-lg" />
              ) : (reportsQuery.data?.items ?? []).length === 0 ? (
                <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                  No reports for this incident yet. Create a report first.
                </p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {(reportsQuery.data?.items ?? []).map((r) => (
                    <li key={r.id}>
                      <button
                        type="button"
                        onClick={() => setPickedReportId(r.id)}
                        className="flex w-full items-center justify-between gap-3 px-3 py-2.5 text-left hover:bg-[var(--color-accent)]"
                      >
                        <span className="text-[13px] font-medium">{formatDate(r.reportDate)}</span>
                        <EntityStatusBadge tone={r.isSigned ? "info" : "default"}>
                          {r.workflowStatus}
                        </EntityStatusBadge>
                      </button>
                    </li>
                  ))}
                </ul>
              )
            ) : (
              /* ── Phase 2: the procedures editor ── */
              <>
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => setEditDxOpen(true)}
                    disabled={!canManage}
                  >
                    <Pencil className="size-4" />
                    Edit Dx Codes
                  </Button>
                  <p className="text-[13px] font-semibold">Procedures Administered in this Report</p>
                  {onMacroText ? (
                    <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                      <input
                        type="checkbox"
                        checked={addMacroText}
                        onChange={(e) => setAddMacroText(e.target.checked)}
                        className="rounded border-[var(--color-border)]"
                      />
                      <span>Add Macro Text to Plan Comments</span>
                    </label>
                  ) : (
                    <span />
                  )}
                </div>

                {proceduresQuery.isLoading ? (
                  <div className="skeleton h-24 rounded-lg" />
                ) : rows.length === 0 ? (
                  <p className="rounded-lg border border-[var(--color-border)] px-3 py-4 text-center text-[13px] text-[var(--color-muted-foreground)]">
                    No procedures yet — pick from the list below.
                  </p>
                ) : (
                  <div className="max-h-56 overflow-auto rounded-lg border border-[var(--color-border)]">
                    <table className="w-full text-[13px]">
                      <thead className="sticky top-0 bg-[var(--color-card)]">
                        <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                          <th className="px-3 py-2 w-28">Code</th>
                          <th className="px-3 py-2">DX Codes</th>
                          <th className="px-3 py-2 w-28">Charge</th>
                          <th className="px-3 py-2 w-10" />
                        </tr>
                      </thead>
                      <tbody>
                        {rows.map((row) => (
                          <tr
                            key={row.key}
                            onDoubleClick={() => canManage && removeRow(row.key)}
                            title={row.description ?? undefined}
                            className="border-b border-[var(--color-border)] last:border-b-0 align-top"
                          >
                            <td className="px-3 py-2 font-medium">{row.code}</td>
                            <td className="px-3 py-2">
                              <div className="flex flex-wrap gap-x-4 gap-y-1">
                                {dxIds.map((dxId) => (
                                  <label
                                    key={dxId}
                                    title={dxLabel.get(dxId)?.description ?? undefined}
                                    className="flex items-center gap-1.5 cursor-pointer"
                                  >
                                    <input
                                      type="checkbox"
                                      checked={row.dxIds.includes(dxId)}
                                      onChange={() => toggleDx(row.key, dxId)}
                                      disabled={!canManage}
                                      className="rounded border-[var(--color-border)]"
                                    />
                                    <span>{dxLabel.get(dxId)?.code ?? `${dxId.slice(0, 8)}…`}</span>
                                  </label>
                                ))}
                              </div>
                            </td>
                            <td className="px-3 py-2">
                              <Input
                                inputMode="decimal"
                                aria-label={`Charge for ${row.code}`}
                                value={row.charge}
                                onChange={(e) => setCharge(row.key, e.target.value)}
                                disabled={!canManage}
                                className="h-8"
                              />
                            </td>
                            <td className="px-3 py-2">
                              {canManage && (
                                <button
                                  type="button"
                                  title="Remove procedure"
                                  aria-label={`Remove ${row.code}`}
                                  onClick={() => removeRow(row.key)}
                                  className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                                >
                                  <X className="size-3.5" />
                                </button>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}

                <p className="text-[12px] text-[var(--color-muted-foreground)]">
                  Select a category and click a procedure to add it to this report. Double-click a
                  row above to remove it.
                </p>

                <div className="grid gap-3 sm:grid-cols-2">
                  <Field id="pp-insurance" label="Insurance">
                    <Combobox
                      id="pp-insurance"
                      label="Insurance"
                      value={insuranceTypeId}
                      onChange={setInsuranceTypeId}
                      options={insuranceOptions ?? []}
                      placeholder="Select insurance…"
                      searchable
                    />
                  </Field>
                  <Field id="pp-category" label="Procedure Category">
                    <Combobox
                      id="pp-category"
                      label="Procedure Category"
                      value={categoryId}
                      onChange={setCategoryId}
                      options={categoryOptions ?? []}
                      placeholder="All categories"
                      searchable
                      clearable
                    />
                  </Field>
                </div>

                {codesQuery.isLoading ? (
                  <div className="skeleton h-32 rounded-lg" />
                ) : (
                  <div className="max-h-56 overflow-auto rounded-lg border border-[var(--color-border)]">
                    <table className="w-full text-[13px]">
                      <thead className="sticky top-0 bg-[var(--color-card)]">
                        <tr className="border-b border-[var(--color-border)] text-left text-[11px] uppercase tracking-wide text-[var(--color-muted-foreground)]">
                          <th className="px-3 py-2 w-28">Code</th>
                          <th className="px-3 py-2">Description</th>
                          <th className="px-3 py-2 w-24">Price</th>
                          <th className="px-3 py-2 w-14" />
                        </tr>
                      </thead>
                      <tbody>
                        {(codesQuery.data?.items ?? []).map((c) => (
                          <tr
                            key={c.id}
                            onDoubleClick={() => canManage && addProcedure(c.id)}
                            className="border-b border-[var(--color-border)] last:border-b-0 hover:bg-[var(--color-accent)]"
                          >
                            <td className="px-3 py-2 font-medium">{c.code}</td>
                            <td className="px-3 py-2">{c.description ?? c.name ?? "—"}</td>
                            <td className="px-3 py-2">
                              {priceByCodeId.has(c.id)
                                ? `$${priceByCodeId.get(c.id)!.toFixed(2)}`
                                : "—"}
                            </td>
                            <td className="px-3 py-2">
                              {canManage && (
                                <button
                                  type="button"
                                  title={`Add ${c.code}`}
                                  aria-label={`Add ${c.code}`}
                                  onClick={() => addProcedure(c.id)}
                                  className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-card)]"
                                >
                                  <Plus className="size-3.5" />
                                </button>
                              )}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </>
            )}
          </DialogBody>

          <DialogFooter>
            {!showPicker && canManage && (
              <Button type="button" onClick={onSave} disabled={saveMutation.isPending}>
                <Save className="size-4" />
                {saveMutation.isPending ? "Saving…" : "Save"}
              </Button>
            )}
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Close
              </Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit the incident's dx set; the incident query invalidation refreshes the checkboxes. */}
      {editDxOpen && (
        <IncidentDialog
          incidentId={incidentId}
          open={editDxOpen}
          onClose={() => {
            setEditDxOpen(false);
            void queryClient.invalidateQueries({ queryKey: ["incident", incidentId] });
          }}
        />
      )}
    </>
  );
}
```

Unused-import note: `ClipboardList` is only used by Tasks 9/10 call sites — remove it from this file's imports if the linter flags it.

- [ ] **Step 3: Verify the frontend compiles**

Run (from `clients/dashboard`): `npm run build`
Expected: success. Fix any mismatch between the `IncidentDialog` props used above and its real signature (from Step 1) — that dialog is the only intentionally-unverified interface in this file.

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/pages/patient-charts/procedures-performed-dialog.tsx
git commit -m "feat(dashboard): Procedures Performed dialog (super bill editor)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 9: Report editor + chart integration

**Files:**
- Modify: `clients/dashboard/src/pages/patient-charts/report-editor.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx`

**Interfaces:**
- Consumes: `ProceduresPerformedDialog` (Task 8), `SUPERBILL_PERMISSIONS` (Task 7), report detail's `incidentId` (already on the dto), chart's `activeIncidentId`/`activeIncident`.
- Produces: a *Procedures Performed* button in the report editor header strip (macro text → Plan field) and a *Procedures* button in the chart-actions row (report-picker phase).

- [ ] **Step 1: Report editor — imports, state, plan-field targeting**

In `report-editor.tsx`:

Add imports (`ClipboardList` into the existing lucide import list):

```tsx
import { SUPERBILL_PERMISSIONS } from "@/lib/patient-permissions";
import { ProceduresPerformedDialog } from "@/pages/patient-charts/procedures-performed-dialog";
```

Below the `DX_IMPORT_FIELDS` block (after `fieldImportsDx`), add:

```tsx
// Field that receives procedure macro text (legacy: Plan Comments). Matched by name
// like DX_IMPORT_FIELDS since clinic fields are per-type copies.
const PLAN_FIELDS = new Set(["plan", "plan comments", "treatment plan"]);
```

Next to the other permission consts (after `canReview`):

```tsx
  const canViewSuperBills = user?.permissions?.includes(SUPERBILL_PERMISSIONS.view) ?? false;
```

Next to the other `useState` hooks:

```tsx
  const [proceduresOpen, setProceduresOpen] = useState(false);
```

After the `groups` memo:

```tsx
  const planFieldId = useMemo(() => {
    const field = (fieldsQuery.data ?? []).find((f) =>
      PLAN_FIELDS.has(f.name.trim().toLowerCase()),
    );
    return field?.id ?? null;
  }, [fieldsQuery.data]);

  // Procedures Performed macro text lands in the Plan field (legacy behavior).
  const onProcedureMacroText = (text: string) => {
    if (planFieldId == null) {
      toast.info("No Plan field on this report type — macro text was not inserted.");
      return;
    }
    insertMacro(planFieldId, text);
  };
```

- [ ] **Step 2: Report editor — button + dialog**

In the patient + status strip JSX (the `div` ending with the `workflowStatus` badge), wrap the badge area so the button sits beside it — replace the closing part:

```tsx
        <span
          className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-[11px] font-semibold uppercase tracking-wider ${
            isSigned
              ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
              : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
          }`}
        >
          {report.workflowStatus}
        </span>
```

with:

```tsx
        <div className="flex items-center gap-2">
          {canViewSuperBills && (
            <Button
              variant="outline"
              size="sm"
              className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
              onClick={() => setProceduresOpen(true)}
            >
              <ClipboardList className="size-4" />
              Procedures Performed
            </Button>
          )}
          <span
            className={`inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-[11px] font-semibold uppercase tracking-wider ${
              isSigned
                ? "bg-[var(--color-primary-soft)] text-[var(--color-primary)]"
                : "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]"
            }`}
          >
            {report.workflowStatus}
          </span>
        </div>
```

Before the component's final closing `</div>`, render the dialog (super bills stay editable after signing — legacy allows it):

```tsx
      {canViewSuperBills && reportId && (
        <ProceduresPerformedDialog
          incidentId={report.incidentId}
          reportId={reportId}
          open={proceduresOpen}
          onClose={() => setProceduresOpen(false)}
          onMacroText={onProcedureMacroText}
        />
      )}
```

(If `report.incidentId` is not on the dashboard's report detail type, it IS on the backend dto — `PatientReportDetailDto.IncidentId` — so add `incidentId: string;` to the detail type in `@/api/reports` rather than working around it. As of this writing it is already there.)

- [ ] **Step 3: Chart — shortcut button + dialog**

In `chart.tsx`:

Add imports (`ClipboardList` into the lucide list):

```tsx
import { SUPERBILL_PERMISSIONS } from "@/lib/patient-permissions";
import { ProceduresPerformedDialog } from "@/pages/patient-charts/procedures-performed-dialog";
```

Next to the other permission consts (around `canExportReports`):

```tsx
  const canViewSuperBills = user?.permissions?.includes(SUPERBILL_PERMISSIONS.view) ?? false;
```

Next to `exportReportsOpen`:

```tsx
  const [proceduresOpen, setProceduresOpen] = useState(false);
```

In the chart-actions row, after the Export Reports button block add:

```tsx
        {canViewSuperBills && (
          <Button
            variant="outline"
            size="sm"
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            disabled={!activeIncident}
            onClick={() => setProceduresOpen(true)}
          >
            <ClipboardList className="size-4" />
            Procedures
          </Button>
        )}
```

Where `ExportReportsDialog` is rendered near the bottom of the component, add alongside it:

```tsx
      {activeIncidentId && (
        <ProceduresPerformedDialog
          incidentId={activeIncidentId}
          reportId={null}
          open={proceduresOpen}
          onClose={() => setProceduresOpen(false)}
        />
      )}
```

- [ ] **Step 4: Verify the frontend compiles**

Run (from `clients/dashboard`): `npm run build`
Expected: success.

- [ ] **Step 5: Commit**

```bash
git add clients/dashboard/src/pages/patient-charts/report-editor.tsx \
        clients/dashboard/src/pages/patient-charts/chart.tsx \
        clients/dashboard/src/api/reports.ts
git commit -m "feat(dashboard): Procedures Performed entry points — report editor + chart shortcut

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 10: Playwright spec (route-mocked)

**Files:**
- Create: `clients/dashboard/tests/patient-charts/procedures-performed.spec.ts`

**Interfaces:**
- Consumes: helper modules `../helpers/api-mocks` (`mockJsonResponse`), `../helpers/auth-seed` (`seedAuthedSession`, `TEST_USER`), `../helpers/shell-mocks` (`installShellMocks`, `paged`) — same as `problems.spec.ts`.
- Produces: e2e coverage for chart-shortcut open → report pick → add procedure → save PUT payload, and the zero-dx guard.

- [ ] **Step 1: Write the spec**

Create `clients/dashboard/tests/patient-charts/procedures-performed.spec.ts`:

```typescript
// E2E coverage for Procedures Performed (route-mocked): chart shortcut → report picker →
// add a procedure (dx defaults checked) → Save PUT payload; zero-dx guard toast.

import { expect, test, type Page } from "@playwright/test";
import { mockJsonResponse } from "../helpers/api-mocks";
import { seedAuthedSession, TEST_USER } from "../helpers/auth-seed";
import { installShellMocks, paged } from "../helpers/shell-mocks";

const PERMS = [
  "Permissions.Patient.SuperBills.View",
  "Permissions.Patient.SuperBills.Manage",
  "Permissions.Patient.Incidents.View",
  "Permissions.Patient.Reports.View",
];

const PATIENT_ID = "00000000-0000-0000-0000-0000000a1111";
const INCIDENT_ID = "00000000-0000-0000-0000-0000000b2222";
const REPORT_ID = "00000000-0000-0000-0000-0000000c3333";
const DX_ID = "00000000-0000-0000-0000-0000000d4444";
const PC_ID = "00000000-0000-0000-0000-0000000e5555";
const IT_ID = "00000000-0000-0000-0000-0000000f6666";

const PATIENT = {
  id: PATIENT_ID,
  patientCode: "P-10293",
  isActive: true,
  demographics: { firstName: "Alice", middleInitial: "Q", lastName: "Vance", dateOfBirth: "1990-04-12", gender: "F" },
  insurance: null,
  lastVisitDate: null,
  nextVisitDate: null,
};

const INCIDENT = {
  id: INCIDENT_ID,
  patientId: PATIENT_ID,
  incidentTypeId: null,
  departmentId: null,
  dateOfInitialVisit: null,
  dateOfLoss: "2026-01-10",
  isClosed: false,
  isTransfer: false,
  isAccident: false,
  accidentType: null,
  accidentState: null,
  patientStatus: "Active",
  diagnosticIds: [DX_ID],
  createdAtUtc: "2026-01-10T08:00:00Z",
  updatedAtUtc: null,
};

const REPORT = {
  id: REPORT_ID,
  incidentId: INCIDENT_ID,
  patientId: PATIENT_ID,
  reportTypeId: 1,
  reportDate: "2026-01-12",
  isSigned: false,
  workflowStatus: "Draft",
  signedByName: null,
  version: 1,
};

const CUSTOM_DX = paged([
  {
    id: DX_ID,
    code: "M99.01",
    description: "Segmental dysfunction of cervical region",
    longDescription: null,
    isChiropractic: true,
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  },
]);

const PROCEDURE_CODES = paged([
  {
    id: PC_ID,
    code: "98940",
    name: "CMT 1-2 regions",
    description: "One to two spinal regions",
    procedureCategoryId: "00000000-0000-0000-0000-000000001111",
    procedureCategoryName: "CMT",
    codeSourceId: null,
    codeSourceName: null,
    macroText: "One to two spinal regions adjusted using CMT",
    isActive: true,
    createdAtUtc: "2026-01-01T00:00:00Z",
    updatedAtUtc: null,
  },
]);

async function mockChartLookups(page: Page, opts?: { dxIds?: string[] }) {
  const incident = { ...INCIDENT, diagnosticIds: opts?.dxIds ?? [DX_ID] };
  await mockJsonResponse(page, "**/api/v1/patient/patients/" + PATIENT_ID, PATIENT);
  await mockJsonResponse(page, `**/api/v1/patient/incidents/${INCIDENT_ID}`, incident);
  await mockJsonResponse(page, "**/api/v1/patient/incidents**", paged([incident]));
  await mockJsonResponse(page, "**/api/v1/patient/reports**", paged([REPORT]));
  await mockJsonResponse(page, "**/api/v1/administration/departments**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/incident-types**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/report-types**", [
    { id: 1, name: "Daily Note", isActive: true },
  ]);
  await mockJsonResponse(page, "**/api/v1/administration/custom-diagnostics**", CUSTOM_DX);
  await mockJsonResponse(page, "**/api/v1/administration/insurance-types**", paged([
    { id: IT_ID, name: "Medicare", isActive: true, procedureCategoryId: null },
  ]));
  await mockJsonResponse(page, "**/api/v1/administration/procedure-categories**", paged([]));
  await mockJsonResponse(page, "**/api/v1/administration/procedure-codes**", PROCEDURE_CODES);
  await mockJsonResponse(
    page,
    `**/api/v1/administration/insurance-types/${IT_ID}/procedures`,
    [{
      id: "00000000-0000-0000-0000-000000009999",
      insuranceTypeId: IT_ID,
      procedureCodeId: PC_ID,
      procedureCode: "98940",
      procedureName: "CMT 1-2 regions",
      procedureCategoryName: "CMT",
      price: 20.0,
      createdAtUtc: "2026-01-01T00:00:00Z",
      updatedAtUtc: null,
    }],
  );
  await mockJsonResponse(page, `**/api/v1/patient/reports/${REPORT_ID}/procedures`, {
    id: null,
    reportId: REPORT_ID,
    isBilled: false,
    billedDateUtc: null,
    procedures: [],
  });
}

test.describe("procedures performed", () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthedSession(page, TEST_USER);
    await installShellMocks(page);
    await mockJsonResponse(page, "**/api/v1/identity/permissions", PERMS);
  });

  test("chart shortcut → pick report → add procedure → save PUT payload", async ({ page }) => {
    await mockChartLookups(page);

    let putBody: unknown = null;
    await page.route(`**/api/v1/patient/reports/${REPORT_ID}/procedures`, async (route) => {
      if (route.request().method() === "PUT") {
        putBody = route.request().postDataJSON();
        await route.fulfill({ status: 204, body: "" });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          id: null, reportId: REPORT_ID, isBilled: false, billedDateUtc: null, procedures: [],
        }),
      });
    });

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await page.getByRole("button", { name: "Procedures", exact: true }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Procedures Performed" });
    await expect(dialog).toBeVisible();

    // Phase 1: pick the report.
    await dialog.getByRole("button", { name: /Draft/ }).click();

    // Phase 2: add the procedure from the picker (dx defaults to checked).
    await dialog.getByRole("button", { name: "Add 98940" }).click();
    await expect(dialog.getByRole("checkbox").last()).toBeChecked();
    await expect(dialog.getByLabel("Charge for 98940")).toHaveValue("20");

    await dialog.getByRole("button", { name: "Save" }).click();

    await expect.poll(() => putBody).not.toBeNull();
    const body = putBody as { reportId: string; procedures: Array<Record<string, unknown>> };
    expect(body.reportId).toBe(REPORT_ID);
    expect(body.procedures).toHaveLength(1);
    expect(body.procedures[0].procedureCodeId).toBe(PC_ID);
    expect(body.procedures[0].code).toBe("98940");
    expect(body.procedures[0].charge).toBe(20);
    expect(body.procedures[0].diagnosticIds).toEqual([DX_ID]);
  });

  test("adding a procedure with zero incident dx shows the guard toast", async ({ page }) => {
    await mockChartLookups(page, { dxIds: [] });

    await page.goto(`/patient-charts/${PATIENT_ID}`);
    await page.getByRole("button", { name: "Procedures", exact: true }).click();

    const dialog = page.getByRole("dialog").filter({ hasText: "Procedures Performed" });
    await dialog.getByRole("button", { name: /Draft/ }).click();
    await dialog.getByRole("button", { name: "Add 98940" }).click();

    await expect(
      page.getByText("Please add at least one DX code before picking procedures."),
    ).toBeVisible();
  });
});
```

Adjust mock URL patterns to the real routes if any differ (verify with Grep in `clients/dashboard/src/api/administration.ts`: insurance-types list route and `listInsuranceTypeProcedures` route).

- [ ] **Step 2: Run the spec**

Run (from `clients/dashboard`): `npx playwright test tests/patient-charts/procedures-performed.spec.ts`
Expected: 2 tests PASS. Iterate on selectors/mocks until green — do not weaken the PUT-payload assertions.

- [ ] **Step 3: Commit**

```bash
git add clients/dashboard/tests/patient-charts/procedures-performed.spec.ts
git commit -m "test(dashboard): Playwright coverage for Procedures Performed dialog

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 11: Docs repo + changelog (golden rule 10)

**Files:**
- Modify (separate repo `github.com/fullstackhero/docs`): patient-chart docs page + `src/content/docs/changelog/` entry.

- [ ] **Step 1: Locate the docs repo**

Check for a local clone: `ls c:/Users/fcoyo/source/repos | grep -i docs` (none existed when this plan was written). If absent, ask the user where the docs repo lives (or whether to clone it) — do NOT skip silently.

- [ ] **Step 2: Write the docs**

In the docs repo's patient-chart page, add a **Procedures Performed** section: what it is (per-report super bill), the two entry points, dx-linking rules (≥ 1 dx per procedure), insurance/category picker with negotiated prices, macro text → Plan field, and the `Patient.SuperBills` View/Manage permissions. Add a changelog entry (follow the existing changelog file format in `src/content/docs/changelog/`) covering: new endpoints `GET/PUT /api/v1/patient/reports/{id}/procedures`, new permissions, the `SuperBillSavedIntegrationEvent` billing seam, and the `ids` filter on custom-diagnostics.

- [ ] **Step 3: Commit/PR the docs repo per its conventions**

---

## Self-review notes (already applied)

- Spec coverage: every spec section maps to a task (domain/config/migration → 1–2, contracts/event/permissions → 3, GET → 4, PUT+event → 5, dx `ids` filter → 6, API+permissions → 7, dialog → 8, entry points → 9, Playwright → 10, docs → 11).
- The two deliberately-verify-at-execution points are called out inline: `IEventBus.PublishAsync` exact signature (Task 5 Step 4 note) and `IncidentDialog` props (Task 8 Step 1). Everything else was copied from the codebase as of commit `cc0184ec`.
- Type consistency: `ReportProcedureItem` (backend) ↔ `ReportProcedureInput` (frontend) intentionally differ in name only across the wire; both serialize `{ procedureCodeId, code, description, charge, diagnosticIds }`.
