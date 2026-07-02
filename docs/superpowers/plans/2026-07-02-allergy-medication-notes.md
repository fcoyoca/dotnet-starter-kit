# Allergy List, Medication List & Patient Notes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port BackChart's Allergy List, Medication List, and Patient Notes into the patient chart as shortcut-button dialogs, backed by new Patient-module aggregates, an Administration drug/reaction/dose-unit catalog with admin CRUD + on-demand RxNav import, and DbMigrator verbs for legacy data.

**Architecture:** Mirror the already-ported Problem List end to end: Patient-module CQRS slices (`Features/v1/...`) → endpoints on the `api/v1/patient` group → dashboard dialogs opened from `chart.tsx`. Reference data follows the Administration Diagnostics pattern (IGlobalEntity + paginated list + admin page). Spec: `docs/superpowers/specs/2026-07-02-allergy-medication-notes-design.md`.

**Tech Stack:** .NET 10, EF Core 10 (PostgreSQL), Mediator 3.x, FluentValidation, xUnit + Shouldly + NSubstitute; React 19 + TanStack Query v5 + Tailwind; DbMigrator (SqlClient → Npgsql).

## Global Constraints

- Handlers `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)` on every await, propagate `CancellationToken` (golden rule 5/7).
- Every command handler + paginated query handler has a `{Name}Validator` (architecture-test enforced, golden rule 8).
- Structured logging only; no string interpolation in log calls (golden rule 6).
- Do NOT touch `src/BuildingBlocks` (golden rule 4). `AddHeroResilience` already exists there.
- `TreatWarningsAsErrors` — the build must be warning-clean.
- File-scoped namespaces, 4-space indent, records for DTOs/commands, `is null` / `is not null`.
- Frontend: pass per-call data through `mutate(arg)` (golden rule 9); PascalCase keys only for URLSearchParams when the API expects them (this codebase uses camelCase query params — match `problems.ts`).
- One commit per task (user's commit-granularity rule).
- All work on branch `clinic-app`.
- Migration creation follows `.agents/skills/create-migration/SKILL.md`: build first, then `dotnet ef migrations add` with `--project src/Host/FSH.Starter.Migrations.PostgreSQL --startup-project src/Host/FSH.Starter.Api --context {X}DbContext --output-dir {X}`.
- Legacy conflict messages verbatim: "You cannot set No Allergies when Active allergies exist." and "You cannot set No Medications when Active medications exist."

**Verification commands used throughout:**

```bash
dotnet build src/FSH.Starter.slnx                                  # warning-clean build
dotnet test src/Tests/Patient.Tests --no-restore                   # patient module tests
dotnet test src/Tests/Administration.Tests --no-restore            # administration tests
dotnet test src/Tests/Architecture.Tests --no-restore              # validator/boundary rules
cd clients/dashboard && npx tsc -b --noEmit && npx eslint src      # frontend typecheck+lint
```

---

### Task 1: Backend — PatientAllergy aggregate + slices

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Domain/PatientAllergy.cs`
- Create: `src/Modules/Patient/Modules.Patient/Data/Configurations/PatientAllergyConfiguration.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/Dtos/PatientAllergyDto.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/CreatePatientAllergyCommand.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/UpdatePatientAllergyCommand.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/GetPatientAllergyByIdQuery.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/SearchPatientAllergiesQuery.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientAllergies/CreatePatientAllergy/{CreatePatientAllergyCommandHandler,CreatePatientAllergyCommandValidator,CreatePatientAllergyEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientAllergies/UpdatePatientAllergy/{UpdatePatientAllergyCommandHandler,UpdatePatientAllergyCommandValidator,UpdatePatientAllergyEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientAllergies/GetPatientAllergyById/{GetPatientAllergyByIdQueryHandler,GetPatientAllergyByIdEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientAllergies/SearchPatientAllergies/{SearchPatientAllergiesQueryHandler,SearchPatientAllergiesQueryValidator,SearchPatientAllergiesEndpoint}.cs`
- Modify: `src/Modules/Patient/Modules.Patient.Contracts/Authorization/PatientPermissions.cs` (add `Allergies` class + `All` entries)
- Modify: `src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs` (DbSet + ApplyConfiguration)
- Modify: `src/Modules/Patient/Modules.Patient/Domain/Patient.cs` (add `SetNoKnownAllergies` / `SetNoKnownMedications`)
- Modify: `src/Modules/Patient/Modules.Patient/PatientModule.cs` (map 4 endpoints)
- Test: `src/Tests/Patient.Tests/Features/PatientAllergyHandlerTests.cs`

**Interfaces:**
- Consumes: existing `PatientDbContext`, `ICurrentUser`, `PagedResponse<T>`, `CustomException`, `NotFoundException`.
- Produces (used by Tasks 5, 11, 12, 13):
  - `PatientAllergyDto(Guid Id, Guid PatientId, string DrugName, string? RxAui, string? Reaction, string? Comments, DateTime DateNoted, bool IsActive, string? CreatedByName, DateTime CreatedAtUtc, string? UpdatedByName, DateTime? UpdatedAtUtc)`
  - `CreatePatientAllergyCommand(Guid PatientId, string DrugName, string? RxAui, string? Reaction, string? Comments, DateTime DateNoted, bool IsActive) : ICommand<Guid>`
  - `UpdatePatientAllergyCommand(Guid AllergyId, string DrugName, string? RxAui, string? Reaction, string? Comments, DateTime DateNoted, bool IsActive) : ICommand<Unit>`
  - `SearchPatientAllergiesQuery(Guid PatientId, bool IncludeInactive = false, int PageNumber = 1, int PageSize = 100) : IQuery<PagedResponse<PatientAllergyDto>>`
  - `GetPatientAllergyByIdQuery(Guid AllergyId) : IQuery<PatientAllergyDto>`
  - Routes: `GET/POST api/v1/patient/allergies`, `GET/PUT api/v1/patient/allergies/{id:guid}`
  - `Patient.SetNoKnownAllergies(bool)`, `Patient.SetNoKnownMedications(bool)` domain methods
  - Permissions: `PatientPermissions.Allergies.{View,Create,Update,Delete}` (`Permissions.Patient.Allergies.*`)

- [ ] **Step 1: Domain entity + Patient flag setters**

`src/Modules/Patient/Modules.Patient/Domain/PatientAllergy.cs`:

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A patient's allergy-list entry (legacy <c>PatientAllergies</c>). Stores the drug name (free text
/// or picked from the Administration drug catalog, legacy <c>paDrugName</c>/<c>paRXAUI</c>), a
/// reaction string built by the SNOMED reaction picker (legacy <c>paReaction varchar(1000)</c>),
/// comments, the clinician-entered "date noted", and an Active/Inactive status — legacy has no
/// delete for allergies, deactivation is the archival path.
/// </summary>
public sealed class PatientAllergy : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public string DrugName { get; private set; } = default!;
    public string? RxAui { get; private set; }
    public string? Reaction { get; private set; }
    public string? Comments { get; private set; }
    public DateTime DateNoted { get; private set; }
    public bool IsActive { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private PatientAllergy() { }

    public static PatientAllergy Create(
        Guid patientId,
        string drugName,
        string? rxAui,
        string? reaction,
        string? comments,
        DateTime dateNoted,
        bool isActive,
        string? createdByUserId,
        string? createdByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        return new PatientAllergy
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DrugName = drugName.Trim(),
            RxAui = Clean(rxAui),
            Reaction = Clean(reaction),
            Comments = Clean(comments),
            DateNoted = dateNoted,
            IsActive = isActive,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string drugName,
        string? rxAui,
        string? reaction,
        string? comments,
        DateTime dateNoted,
        bool isActive,
        string? updatedByUserId,
        string? updatedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        DrugName = drugName.Trim();
        RxAui = Clean(rxAui);
        Reaction = Clean(reaction);
        Comments = Clean(comments);
        DateNoted = dateNoted;
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
```

In `src/Modules/Patient/Modules.Patient/Domain/Patient.cs`, add below the existing `Restore()`/flag-related methods (anywhere after `Update`), two focused setters used by the allergy/medication handlers and the Set-flag commands:

```csharp
    /// <summary>Chart "Set No Allergies" checkbox; auto-cleared when an active allergy is saved.</summary>
    public void SetNoKnownAllergies(bool value)
    {
        HasNoKnownAllergies = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Chart "Set No Medications" checkbox; auto-cleared when an active medication is saved.</summary>
    public void SetNoKnownMedications(bool value)
    {
        HasNoKnownMedications = value;
        UpdatedAtUtc = DateTime.UtcNow;
    }
```

- [ ] **Step 2: EF configuration + DbSet**

`src/Modules/Patient/Modules.Patient/Data/Configurations/PatientAllergyConfiguration.cs`:

```csharp
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientAllergyConfiguration : IEntityTypeConfiguration<PatientAllergy>
{
    public void Configure(EntityTypeBuilder<PatientAllergy> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientAllergies");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.DrugName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RxAui).HasMaxLength(12);
        builder.Property(x => x.Reaction).HasMaxLength(1000);
        builder.Property(x => x.Comments).HasMaxLength(4000);
        builder.Property(x => x.DateNoted).HasColumnType("date").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsActive });

        builder.Ignore(x => x.DomainEvents);
    }
}
```

In `src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs` add the DbSet next to `PatientProblems`:

```csharp
    public DbSet<Domain.PatientAllergy> PatientAllergies => Set<Domain.PatientAllergy>();
```

and in `OnModelCreating`, before `base.OnModelCreating(modelBuilder);`:

```csharp
        modelBuilder.ApplyConfiguration(new PatientAllergyConfiguration());
```

- [ ] **Step 3: Contracts (DTO, commands, queries)**

`src/Modules/Patient/Modules.Patient.Contracts/Dtos/PatientAllergyDto.cs`:

```csharp
namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientAllergyDto(
    Guid Id,
    Guid PatientId,
    string DrugName,
    string? RxAui,
    string? Reaction,
    string? Comments,
    DateTime DateNoted,
    bool IsActive,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
```

`src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/CreatePatientAllergyCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record CreatePatientAllergyCommand(
    Guid PatientId,
    string DrugName,
    string? RxAui,
    string? Reaction,
    string? Comments,
    DateTime DateNoted,
    bool IsActive = true) : ICommand<Guid>;
```

`src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/UpdatePatientAllergyCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record UpdatePatientAllergyCommand(
    Guid AllergyId,
    string DrugName,
    string? RxAui,
    string? Reaction,
    string? Comments,
    DateTime DateNoted,
    bool IsActive) : ICommand<Unit>;
```

`src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/GetPatientAllergyByIdQuery.cs`:

```csharp
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record GetPatientAllergyByIdQuery(Guid AllergyId) : IQuery<PatientAllergyDto>;
```

`src/Modules/Patient/Modules.Patient.Contracts/v1/PatientAllergies/SearchPatientAllergiesQuery.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record SearchPatientAllergiesQuery(
    Guid PatientId,
    bool IncludeInactive = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientAllergyDto>>;
```

- [ ] **Step 4: Permissions**

In `src/Modules/Patient/Modules.Patient.Contracts/Authorization/PatientPermissions.cs` add after the `Problems` class:

```csharp
    public static class Allergies
    {
        public const string Resource = "Patient.Allergies";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }
```

and append to the `All` list:

```csharp
        new("View Allergies",   ActionConstants.View,   Allergies.Resource, IsBasic: true),
        new("Create Allergies", ActionConstants.Create, Allergies.Resource),
        new("Update Allergies", ActionConstants.Update, Allergies.Resource),
        new("Delete Allergies", ActionConstants.Delete, Allergies.Resource),
```

- [ ] **Step 5: Write failing handler tests**

`src/Tests/Patient.Tests/Features/PatientAllergyHandlerTests.cs` (the `CreateContext` helper matches `CreatePatientCommandHandlerTests`; the `SeedPatient` helper inserts a patient the flag-clearing logic can load):

```csharp
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;
using FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;
using FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class PatientAllergyHandlerTests
{
    private static PatientDbContext CreateContext(string dbName)
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

    private static ICurrentUser User()
    {
        var user = Substitute.For<ICurrentUser>();
        user.GetUserId().Returns(Guid.NewGuid());
        user.Name.Returns("Test User");
        return user;
    }

    private static async Task<Guid> SeedPatientAsync(PatientDbContext db, bool noKnownAllergies = false)
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-1000", true,
            PatientDemographics.Create(false, null, null, null, null, null, null, null, null),
            PatientContact.Create(null, null, null, null, null, null, null, null, null, null),
            PatientPhi.Create("John", "Doe", null, new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, null, Substitute.For<IPhiEncryptor>()),
            null, null, null, null,
            hasNoKnownProblems: false,
            hasNoKnownMedications: false,
            hasNoKnownAllergies: noKnownAllergies,
            receivesEmailReminders: false,
            lastVisitDate: null, nextVisitDate: null);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient.Id;
    }

    [Fact]
    public async Task Create_Should_Persist_And_Clear_NoKnownAllergies_When_Active()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db, noKnownAllergies: true);
        var sut = new CreatePatientAllergyCommandHandler(db, User());

        Guid id = await sut.Handle(new CreatePatientAllergyCommand(
            patientId, "Penicillin", "12345", "Rash", null,
            new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)), CancellationToken.None);

        var saved = await db.PatientAllergies.FindAsync(id);
        saved.ShouldNotBeNull();
        saved!.DrugName.ShouldBe("Penicillin");
        saved.IsActive.ShouldBeTrue();
        (await db.Patients.FindAsync(patientId))!.HasNoKnownAllergies.ShouldBeFalse();
    }

    [Fact]
    public async Task Create_Should_Keep_NoKnownAllergies_When_Inactive()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db, noKnownAllergies: true);
        var sut = new CreatePatientAllergyCommandHandler(db, User());

        await sut.Handle(new CreatePatientAllergyCommand(
            patientId, "Aspirin", null, null, null,
            new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc), IsActive: false), CancellationToken.None);

        (await db.Patients.FindAsync(patientId))!.HasNoKnownAllergies.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_Should_Change_Fields_And_Clear_Flag_When_Reactivated()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var allergy = PatientAllergy.Create(patientId, "Latex", null, null, null,
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), isActive: false, null, null);
        db.PatientAllergies.Add(allergy);
        await db.SaveChangesAsync();
        (await db.Patients.FindAsync(patientId))!.SetNoKnownAllergies(true);
        await db.SaveChangesAsync();
        var sut = new UpdatePatientAllergyCommandHandler(db, User());

        await sut.Handle(new UpdatePatientAllergyCommand(
            allergy.Id, "Latex", null, "Hives", "worse now",
            new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc), IsActive: true), CancellationToken.None);

        var saved = await db.PatientAllergies.FindAsync(allergy.Id);
        saved!.Reaction.ShouldBe("Hives");
        saved.IsActive.ShouldBeTrue();
        (await db.Patients.FindAsync(patientId))!.HasNoKnownAllergies.ShouldBeFalse();
    }

    [Fact]
    public async Task Search_Should_Exclude_Inactive_By_Default()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        db.PatientAllergies.Add(PatientAllergy.Create(patientId, "Active drug", null, null, null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, null, null));
        db.PatientAllergies.Add(PatientAllergy.Create(patientId, "Inactive drug", null, null, null,
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), false, null, null));
        await db.SaveChangesAsync();
        var sut = new SearchPatientAllergiesQueryHandler(db);

        PagedResponse<FSH.Modules.Patient.Contracts.Dtos.PatientAllergyDto> defaults =
            await sut.Handle(new SearchPatientAllergiesQuery(patientId), CancellationToken.None);
        PagedResponse<FSH.Modules.Patient.Contracts.Dtos.PatientAllergyDto> all =
            await sut.Handle(new SearchPatientAllergiesQuery(patientId, IncludeInactive: true), CancellationToken.None);

        defaults.Items.Count.ShouldBe(1);
        defaults.Items[0].DrugName.ShouldBe("Active drug");
        all.Items.Count.ShouldBe(2);
    }
}
```

> NOTE: `PatientDemographics.Create` / `PatientContact.Create` / `PatientPhi.Create` signatures above are the plan author's best reading — before running, open those three domain files and match the actual factory parameter lists exactly (they are `internal static`, visible via `InternalsVisibleTo("Patient.Tests")`). If an existing test/fixture already builds a `Patient` (search Patient.Tests for `Patient.Create(`), reuse that helper instead.

- [ ] **Step 6: Run tests — expect compile failure (handlers don't exist)**

```bash
dotnet test src/Tests/Patient.Tests --no-restore
```

Expected: FAILS to compile — `CreatePatientAllergyCommandHandler` not found.

- [ ] **Step 7: Implement handlers, validators, endpoints**

`.../Features/v1/PatientAllergies/CreatePatientAllergy/CreatePatientAllergyCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;

public sealed class CreatePatientAllergyCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientAllergyCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientAllergyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        PatientAllergy allergy = PatientAllergy.Create(
            command.PatientId,
            command.DrugName,
            command.RxAui,
            command.Reaction,
            command.Comments,
            command.DateNoted,
            command.IsActive,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.PatientAllergies.Add(allergy);

        // Legacy behavior: saving an active allergy clears the patient's "No Allergies" flag.
        if (command.IsActive && patient.HasNoKnownAllergies)
        {
            patient.SetNoKnownAllergies(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return allergy.Id;
    }
}
```

> If `NotFoundException` lives in a different namespace, check an existing handler (e.g. `UpdatePatientProblemCommandHandler`) and copy its `using` for it.

`.../CreatePatientAllergy/CreatePatientAllergyCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;

public sealed class CreatePatientAllergyCommandValidator : AbstractValidator<CreatePatientAllergyCommand>
{
    public CreatePatientAllergyCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.Reaction).MaximumLength(1000);
        RuleFor(x => x.Comments).MaximumLength(4000);
        RuleFor(x => x.DateNoted).NotEmpty();
    }
}
```

`.../CreatePatientAllergy/CreatePatientAllergyEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;

public static class CreatePatientAllergyEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientAllergyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/allergies",
                async (CreatePatientAllergyCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientAllergy")
            .WithSummary("Add an allergy to a patient's allergy list")
            .RequirePermission(PatientPermissions.Allergies.Create);
    }
}
```

`.../UpdatePatientAllergy/UpdatePatientAllergyCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;

public sealed class UpdatePatientAllergyCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientAllergyCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientAllergyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientAllergy allergy = await dbContext.PatientAllergies
            .FirstOrDefaultAsync(a => a.Id == command.AllergyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Allergy {command.AllergyId} not found.");

        allergy.Update(
            command.DrugName,
            command.RxAui,
            command.Reaction,
            command.Comments,
            command.DateNoted,
            command.IsActive,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        if (command.IsActive)
        {
            Domain.Patient? patient = await dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == allergy.PatientId, cancellationToken)
                .ConfigureAwait(false);
            if (patient is not null && patient.HasNoKnownAllergies)
            {
                patient.SetNoKnownAllergies(false);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
```

`.../UpdatePatientAllergy/UpdatePatientAllergyCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;

public sealed class UpdatePatientAllergyCommandValidator : AbstractValidator<UpdatePatientAllergyCommand>
{
    public UpdatePatientAllergyCommandValidator()
    {
        RuleFor(x => x.AllergyId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.Reaction).MaximumLength(1000);
        RuleFor(x => x.Comments).MaximumLength(4000);
        RuleFor(x => x.DateNoted).NotEmpty();
    }
}
```

`.../UpdatePatientAllergy/UpdatePatientAllergyEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;

public static class UpdatePatientAllergyEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientAllergyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/allergies/{id:guid}",
                async (Guid id, UpdatePatientAllergyCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (id != command.AllergyId)
                    {
                        return Results.BadRequest("Route id and body AllergyId do not match.");
                    }
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientAllergy")
            .WithSummary("Update an allergy-list entry (including Active/Inactive)")
            .RequirePermission(PatientPermissions.Allergies.Update);
    }
}
```

> Match the id-vs-body check style used by `UpdatePatientProblemEndpoint` — open it first and mirror it exactly (if it doesn't compare ids, don't add the check here either).

`.../GetPatientAllergyById/GetPatientAllergyByIdQueryHandler.cs`:

```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.GetPatientAllergyById;

public sealed class GetPatientAllergyByIdQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetPatientAllergyByIdQuery, PatientAllergyDto>
{
    public async ValueTask<PatientAllergyDto> Handle(GetPatientAllergyByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        PatientAllergyDto? dto = await dbContext.PatientAllergies
            .AsNoTracking()
            .Where(a => a.Id == query.AllergyId)
            .Select(a => new PatientAllergyDto(
                a.Id, a.PatientId, a.DrugName, a.RxAui, a.Reaction, a.Comments,
                a.DateNoted, a.IsActive, a.CreatedByName, a.CreatedAtUtc, a.UpdatedByName, a.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto ?? throw new NotFoundException($"Allergy {query.AllergyId} not found.");
    }
}
```

`.../GetPatientAllergyById/GetPatientAllergyByIdEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.GetPatientAllergyById;

public static class GetPatientAllergyByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientAllergyByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/allergies/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientAllergyByIdQuery(id), ct)))
            .WithName("GetPatientAllergyById")
            .WithSummary("Get a single allergy-list entry")
            .RequirePermission(PatientPermissions.Allergies.View);
    }
}
```

`.../SearchPatientAllergies/SearchPatientAllergiesQueryHandler.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;

public sealed class SearchPatientAllergiesQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientAllergiesQuery, PagedResponse<PatientAllergyDto>>
{
    public async ValueTask<PagedResponse<PatientAllergyDto>> Handle(
        SearchPatientAllergiesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientAllergy> q = dbContext.PatientAllergies
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeInactive)
        {
            q = q.Where(x => x.IsActive);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientAllergyDto> items = await q
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.DateNoted)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new PatientAllergyDto(
                x.Id, x.PatientId, x.DrugName, x.RxAui, x.Reaction, x.Comments,
                x.DateNoted, x.IsActive, x.CreatedByName, x.CreatedAtUtc, x.UpdatedByName, x.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientAllergyDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
```

`.../SearchPatientAllergies/SearchPatientAllergiesQueryValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;

public sealed class SearchPatientAllergiesQueryValidator : AbstractValidator<SearchPatientAllergiesQuery>
{
    public SearchPatientAllergiesQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
```

`.../SearchPatientAllergies/SearchPatientAllergiesEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;

public static class SearchPatientAllergiesEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientAllergiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/allergies",
                async (
                    Guid patientId,
                    bool? includeInactive,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientAllergiesQuery(
                            patientId,
                            includeInactive ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientAllergies")
            .WithSummary("Search a patient's allergy list")
            .RequirePermission(PatientPermissions.Allergies.View);
    }
}
```

In `src/Modules/Patient/Modules.Patient/PatientModule.cs`, add the four usings and map after the Problem endpoints:

```csharp
        // Allergy endpoints — literal /allergies collection route before /allergies/{id:guid}
        group.MapSearchPatientAllergiesEndpoint();
        group.MapCreatePatientAllergyEndpoint();
        group.MapGetPatientAllergyByIdEndpoint();
        group.MapUpdatePatientAllergyEndpoint();
```

- [ ] **Step 8: Run tests + build — expect green**

```bash
dotnet build src/FSH.Starter.slnx
dotnet test src/Tests/Patient.Tests --no-restore
dotnet test src/Tests/Architecture.Tests --no-restore
```

Expected: build 0 warnings; all tests PASS (architecture tests confirm validators exist).

- [ ] **Step 9: Commit**

```bash
git add src/Modules/Patient src/Tests/Patient.Tests
git commit -m "feat(patient): PatientAllergy aggregate + CRUD slices with no-allergies flag clearing"
```

---

### Task 2: Backend — PatientMedication aggregate + slices

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Domain/PatientMedication.cs`
- Create: `src/Modules/Patient/Modules.Patient/Data/Configurations/PatientMedicationConfiguration.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/Dtos/PatientMedicationDto.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientMedications/{CreatePatientMedicationCommand,UpdatePatientMedicationCommand,GetPatientMedicationByIdQuery,SearchPatientMedicationsQuery}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientMedications/CreatePatientMedication/{CreatePatientMedicationCommandHandler,CreatePatientMedicationCommandValidator,CreatePatientMedicationEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientMedications/UpdatePatientMedication/{UpdatePatientMedicationCommandHandler,UpdatePatientMedicationCommandValidator,UpdatePatientMedicationEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientMedications/GetPatientMedicationById/{GetPatientMedicationByIdQueryHandler,GetPatientMedicationByIdEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientMedications/SearchPatientMedications/{SearchPatientMedicationsQueryHandler,SearchPatientMedicationsQueryValidator,SearchPatientMedicationsEndpoint}.cs`
- Modify: `PatientPermissions.cs` (add `Medications`), `PatientDbContext.cs` (DbSet + config), `PatientModule.cs` (map endpoints)
- Test: `src/Tests/Patient.Tests/Features/PatientMedicationHandlerTests.cs`

**Interfaces:**
- Consumes: `Patient.SetNoKnownMedications(bool)` from Task 1.
- Produces (used by Tasks 4, 5, 11, 12, 14):
  - `PatientMedicationDto(Guid Id, Guid PatientId, string DrugName, string? RxAui, string? RxCode, string? Ndc, string? Prescriber, DateTime StartDate, DateTime? EndDate, decimal? DoseValue, int? DoseUnitId, decimal? DosePeriodValue, string? DosePeriodUnit, string? Instructions, string? Indication, bool IsActive, string? CreatedByName, DateTime CreatedAtUtc, string? UpdatedByName, DateTime? UpdatedAtUtc)`
  - `CreatePatientMedicationCommand(Guid PatientId, string DrugName, string? RxAui, string? RxCode, string? Ndc, string? Prescriber, DateTime StartDate, DateTime? EndDate, decimal? DoseValue, int? DoseUnitId, decimal? DosePeriodValue, string? DosePeriodUnit, string? Instructions, string? Indication, bool IsActive = true) : ICommand<Guid>`
  - `UpdatePatientMedicationCommand(Guid MedicationId, <same fields as create minus PatientId>) : ICommand<Unit>`
  - `SearchPatientMedicationsQuery(Guid PatientId, bool IncludeInactive = false, int PageNumber = 1, int PageSize = 100) : IQuery<PagedResponse<PatientMedicationDto>>`
  - Routes: `GET/POST api/v1/patient/medications`, `GET/PUT api/v1/patient/medications/{id:guid}`
  - Permissions: `PatientPermissions.Medications.{View,Create,Update,Delete}`

This task is structurally identical to Task 1 with the medication field set. Complete code follows.

- [ ] **Step 1: Domain entity**

`src/Modules/Patient/Modules.Patient/Domain/PatientMedication.cs`:

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A patient's medication-list entry (legacy <c>PatientMedications</c>). Drug identity comes from the
/// Administration drug catalog (or free text): <c>RxAui</c> (legacy <c>pmRXAUI</c>), <c>RxCode</c>
/// (RXCUI — drives the MedlinePlus "Info" link), <c>Ndc</c>. Dose is split value/unit/period the way
/// legacy stored it (<c>pmDoseValue</c>/<c>pmDoseUnitID</c>/<c>pmDosePeriodValue</c>/<c>pmDosePeriodUnit</c>);
/// <c>DoseUnitId</c> is a bare cross-schema id into the Administration MedicationDoseUnits lookup.
/// Active/Inactive instead of delete, matching legacy.
/// </summary>
public sealed class PatientMedication : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public string DrugName { get; private set; } = default!;
    public string? RxAui { get; private set; }
    public string? RxCode { get; private set; }
    public string? Ndc { get; private set; }
    public string? Prescriber { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public decimal? DoseValue { get; private set; }
    public int? DoseUnitId { get; private set; }
    public decimal? DosePeriodValue { get; private set; }
    public string? DosePeriodUnit { get; private set; }
    public string? Instructions { get; private set; }
    public string? Indication { get; private set; }
    public bool IsActive { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private PatientMedication() { }

    public static PatientMedication Create(
        Guid patientId,
        string drugName,
        string? rxAui,
        string? rxCode,
        string? ndc,
        string? prescriber,
        DateTime startDate,
        DateTime? endDate,
        decimal? doseValue,
        int? doseUnitId,
        decimal? dosePeriodValue,
        string? dosePeriodUnit,
        string? instructions,
        string? indication,
        bool isActive,
        string? createdByUserId,
        string? createdByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        return new PatientMedication
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DrugName = drugName.Trim(),
            RxAui = Clean(rxAui),
            RxCode = Clean(rxCode),
            Ndc = Clean(ndc),
            Prescriber = Clean(prescriber),
            StartDate = startDate,
            EndDate = endDate,
            DoseValue = doseValue,
            DoseUnitId = doseUnitId,
            DosePeriodValue = dosePeriodValue,
            DosePeriodUnit = Clean(dosePeriodUnit),
            Instructions = Clean(instructions),
            Indication = Clean(indication),
            IsActive = isActive,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string drugName,
        string? rxAui,
        string? rxCode,
        string? ndc,
        string? prescriber,
        DateTime startDate,
        DateTime? endDate,
        decimal? doseValue,
        int? doseUnitId,
        decimal? dosePeriodValue,
        string? dosePeriodUnit,
        string? instructions,
        string? indication,
        bool isActive,
        string? updatedByUserId,
        string? updatedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drugName);
        DrugName = drugName.Trim();
        RxAui = Clean(rxAui);
        RxCode = Clean(rxCode);
        Ndc = Clean(ndc);
        Prescriber = Clean(prescriber);
        StartDate = startDate;
        EndDate = endDate;
        DoseValue = doseValue;
        DoseUnitId = doseUnitId;
        DosePeriodValue = dosePeriodValue;
        DosePeriodUnit = Clean(dosePeriodUnit);
        Instructions = Clean(instructions);
        Indication = Clean(indication);
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
```

- [ ] **Step 2: EF configuration + DbSet**

`.../Data/Configurations/PatientMedicationConfiguration.cs`:

```csharp
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientMedicationConfiguration : IEntityTypeConfiguration<PatientMedication>
{
    public void Configure(EntityTypeBuilder<PatientMedication> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientMedications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.DrugName).HasMaxLength(512).IsRequired();
        builder.Property(x => x.RxAui).HasMaxLength(12);
        builder.Property(x => x.RxCode).HasMaxLength(12);
        builder.Property(x => x.Ndc).HasMaxLength(24);
        builder.Property(x => x.Prescriber).HasMaxLength(256);
        builder.Property(x => x.StartDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.DoseValue).HasPrecision(12, 3);
        builder.Property(x => x.DosePeriodValue).HasPrecision(12, 3);
        builder.Property(x => x.DosePeriodUnit).HasMaxLength(16);
        builder.Property(x => x.Instructions).HasMaxLength(4000);
        builder.Property(x => x.Indication).HasMaxLength(4000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsActive });

        builder.Ignore(x => x.DomainEvents);
    }
}
```

`PatientDbContext.cs`: add DbSet + ApplyConfiguration:

```csharp
    public DbSet<Domain.PatientMedication> PatientMedications => Set<Domain.PatientMedication>();
```

```csharp
        modelBuilder.ApplyConfiguration(new PatientMedicationConfiguration());
```

- [ ] **Step 3: Contracts**

`Modules.Patient.Contracts/Dtos/PatientMedicationDto.cs`:

```csharp
namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientMedicationDto(
    Guid Id,
    Guid PatientId,
    string DrugName,
    string? RxAui,
    string? RxCode,
    string? Ndc,
    string? Prescriber,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? DoseValue,
    int? DoseUnitId,
    decimal? DosePeriodValue,
    string? DosePeriodUnit,
    string? Instructions,
    string? Indication,
    bool IsActive,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
```

`Modules.Patient.Contracts/v1/PatientMedications/CreatePatientMedicationCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record CreatePatientMedicationCommand(
    Guid PatientId,
    string DrugName,
    string? RxAui,
    string? RxCode,
    string? Ndc,
    string? Prescriber,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? DoseValue,
    int? DoseUnitId,
    decimal? DosePeriodValue,
    string? DosePeriodUnit,
    string? Instructions,
    string? Indication,
    bool IsActive = true) : ICommand<Guid>;
```

`.../UpdatePatientMedicationCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record UpdatePatientMedicationCommand(
    Guid MedicationId,
    string DrugName,
    string? RxAui,
    string? RxCode,
    string? Ndc,
    string? Prescriber,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? DoseValue,
    int? DoseUnitId,
    decimal? DosePeriodValue,
    string? DosePeriodUnit,
    string? Instructions,
    string? Indication,
    bool IsActive) : ICommand<Unit>;
```

`.../GetPatientMedicationByIdQuery.cs`:

```csharp
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record GetPatientMedicationByIdQuery(Guid MedicationId) : IQuery<PatientMedicationDto>;
```

`.../SearchPatientMedicationsQuery.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record SearchPatientMedicationsQuery(
    Guid PatientId,
    bool IncludeInactive = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientMedicationDto>>;
```

- [ ] **Step 4: Permissions**

In `PatientPermissions.cs` add:

```csharp
    public static class Medications
    {
        public const string Resource = "Patient.Medications";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }
```

and to `All`:

```csharp
        new("View Medications",   ActionConstants.View,   Medications.Resource, IsBasic: true),
        new("Create Medications", ActionConstants.Create, Medications.Resource),
        new("Update Medications", ActionConstants.Update, Medications.Resource),
        new("Delete Medications", ActionConstants.Delete, Medications.Resource),
```

- [ ] **Step 5: Write failing handler tests**

`src/Tests/Patient.Tests/Features/PatientMedicationHandlerTests.cs` — reuse the exact `CreateContext`, `User()`, and `SeedPatientAsync` helpers from `PatientAllergyHandlerTests` (copy them; the seed helper takes `noKnownMedications` instead):

```csharp
// usings identical to PatientAllergyHandlerTests plus:
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;
using FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;
using FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;

namespace Patient.Tests.Features;

public sealed class PatientMedicationHandlerTests
{
    // CreateContext + User() copied verbatim from PatientAllergyHandlerTests.
    // SeedPatientAsync(db, bool noKnownMedications = false) — same as allergy version but passes
    // hasNoKnownMedications: noKnownMedications and hasNoKnownAllergies: false.

    private static CreatePatientMedicationCommand ValidCreate(Guid patientId, bool isActive = true) => new(
        patientId, "Lisinopril 10 MG Oral Tablet", "1998", "314076", null, "Dr. Smith",
        new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), null,
        10m, 1, 1m, "d", "Take with food", "Hypertension", isActive);

    [Fact]
    public async Task Create_Should_Persist_And_Clear_NoKnownMedications_When_Active()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db, noKnownMedications: true);
        var sut = new CreatePatientMedicationCommandHandler(db, User());

        Guid id = await sut.Handle(ValidCreate(patientId), CancellationToken.None);

        var saved = await db.PatientMedications.FindAsync(id);
        saved.ShouldNotBeNull();
        saved!.DrugName.ShouldBe("Lisinopril 10 MG Oral Tablet");
        saved.DoseValue.ShouldBe(10m);
        saved.DosePeriodUnit.ShouldBe("d");
        (await db.Patients.FindAsync(patientId))!.HasNoKnownMedications.ShouldBeFalse();
    }

    [Fact]
    public async Task Create_Should_Keep_NoKnownMedications_When_Inactive()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db, noKnownMedications: true);
        var sut = new CreatePatientMedicationCommandHandler(db, User());

        await sut.Handle(ValidCreate(patientId, isActive: false), CancellationToken.None);

        (await db.Patients.FindAsync(patientId))!.HasNoKnownMedications.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_Should_Change_Fields()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var sut = new CreatePatientMedicationCommandHandler(db, User());
        Guid id = await sut.Handle(ValidCreate(patientId), CancellationToken.None);
        var update = new UpdatePatientMedicationCommandHandler(db, User());

        await update.Handle(new UpdatePatientMedicationCommand(
            id, "Lisinopril 20 MG Oral Tablet", "1998", "314077", null, "Dr. Smith",
            new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            20m, 1, 1m, "d", null, "Hypertension", IsActive: false), CancellationToken.None);

        var saved = await db.PatientMedications.FindAsync(id);
        saved!.DoseValue.ShouldBe(20m);
        saved.EndDate.ShouldNotBeNull();
        saved.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task Search_Should_Exclude_Inactive_By_Default()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientMedicationCommandHandler(db, User());
        await create.Handle(ValidCreate(patientId, isActive: true), CancellationToken.None);
        await create.Handle(ValidCreate(patientId, isActive: false), CancellationToken.None);
        var sut = new SearchPatientMedicationsQueryHandler(db);

        var defaults = await sut.Handle(new SearchPatientMedicationsQuery(patientId), CancellationToken.None);
        var all = await sut.Handle(new SearchPatientMedicationsQuery(patientId, IncludeInactive: true), CancellationToken.None);

        defaults.Items.Count.ShouldBe(1);
        all.Items.Count.ShouldBe(2);
    }
}
```

- [ ] **Step 6: Run — expect compile failure**

```bash
dotnet test src/Tests/Patient.Tests --no-restore
```

Expected: FAILS to compile — medication handlers not found.

- [ ] **Step 7: Implement handlers, validators, endpoints**

`.../CreatePatientMedication/CreatePatientMedicationCommandHandler.cs` — same shape as the allergy create handler:

```csharp
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;

public sealed class CreatePatientMedicationCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientMedicationCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientMedicationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        PatientMedication medication = PatientMedication.Create(
            command.PatientId, command.DrugName, command.RxAui, command.RxCode, command.Ndc,
            command.Prescriber, command.StartDate, command.EndDate,
            command.DoseValue, command.DoseUnitId, command.DosePeriodValue, command.DosePeriodUnit,
            command.Instructions, command.Indication, command.IsActive,
            currentUser.GetUserId().ToString(), currentUser.Name);

        dbContext.PatientMedications.Add(medication);

        if (command.IsActive && patient.HasNoKnownMedications)
        {
            patient.SetNoKnownMedications(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return medication.Id;
    }
}
```

`.../CreatePatientMedication/CreatePatientMedicationCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;

public sealed class CreatePatientMedicationCommandValidator : AbstractValidator<CreatePatientMedicationCommand>
{
    public CreatePatientMedicationCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.RxCode).MaximumLength(12);
        RuleFor(x => x.Ndc).MaximumLength(24);
        RuleFor(x => x.Prescriber).MaximumLength(256);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.DoseValue).GreaterThan(0).When(x => x.DoseValue.HasValue);
        RuleFor(x => x.DosePeriodValue).GreaterThan(0).When(x => x.DosePeriodValue.HasValue);
        RuleFor(x => x.DosePeriodUnit).MaximumLength(16);
        RuleFor(x => x.Instructions).MaximumLength(4000);
        RuleFor(x => x.Indication).MaximumLength(4000);
    }
}
```

`.../CreatePatientMedication/CreatePatientMedicationEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;

public static class CreatePatientMedicationEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientMedicationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/medications",
                async (CreatePatientMedicationCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientMedication")
            .WithSummary("Add a medication to a patient's medication list")
            .RequirePermission(PatientPermissions.Medications.Create);
    }
}
```

`.../UpdatePatientMedication/UpdatePatientMedicationCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;

public sealed class UpdatePatientMedicationCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientMedicationCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientMedicationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientMedication medication = await dbContext.PatientMedications
            .FirstOrDefaultAsync(m => m.Id == command.MedicationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Medication {command.MedicationId} not found.");

        medication.Update(
            command.DrugName, command.RxAui, command.RxCode, command.Ndc,
            command.Prescriber, command.StartDate, command.EndDate,
            command.DoseValue, command.DoseUnitId, command.DosePeriodValue, command.DosePeriodUnit,
            command.Instructions, command.Indication, command.IsActive,
            currentUser.GetUserId().ToString(), currentUser.Name);

        if (command.IsActive)
        {
            Domain.Patient? patient = await dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == medication.PatientId, cancellationToken)
                .ConfigureAwait(false);
            if (patient is not null && patient.HasNoKnownMedications)
            {
                patient.SetNoKnownMedications(false);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
```

`.../UpdatePatientMedication/UpdatePatientMedicationCommandValidator.cs` — same rules as the create validator with `MedicationId` instead of `PatientId`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;

public sealed class UpdatePatientMedicationCommandValidator : AbstractValidator<UpdatePatientMedicationCommand>
{
    public UpdatePatientMedicationCommandValidator()
    {
        RuleFor(x => x.MedicationId).NotEmpty();
        RuleFor(x => x.DrugName).NotEmpty().MaximumLength(512);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.RxCode).MaximumLength(12);
        RuleFor(x => x.Ndc).MaximumLength(24);
        RuleFor(x => x.Prescriber).MaximumLength(256);
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).When(x => x.EndDate.HasValue);
        RuleFor(x => x.DoseValue).GreaterThan(0).When(x => x.DoseValue.HasValue);
        RuleFor(x => x.DosePeriodValue).GreaterThan(0).When(x => x.DosePeriodValue.HasValue);
        RuleFor(x => x.DosePeriodUnit).MaximumLength(16);
        RuleFor(x => x.Instructions).MaximumLength(4000);
        RuleFor(x => x.Indication).MaximumLength(4000);
    }
}
```

`.../UpdatePatientMedication/UpdatePatientMedicationEndpoint.cs` — PUT `/medications/{id:guid}`, name `UpdatePatientMedication`, permission `PatientPermissions.Medications.Update`, identical id-check shape to `UpdatePatientAllergyEndpoint` (Task 1 Step 7) with `command.MedicationId` in place of `command.AllergyId`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;

public static class UpdatePatientMedicationEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientMedicationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/medications/{id:guid}",
                async (Guid id, UpdatePatientMedicationCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (id != command.MedicationId)
                    {
                        return Results.BadRequest("Route id and body MedicationId do not match.");
                    }
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientMedication")
            .WithSummary("Update a medication-list entry (including Active/Inactive)")
            .RequirePermission(PatientPermissions.Medications.Update);
    }
}
```

`.../GetPatientMedicationById/GetPatientMedicationByIdQueryHandler.cs`:

```csharp
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.GetPatientMedicationById;

public sealed class GetPatientMedicationByIdQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetPatientMedicationByIdQuery, PatientMedicationDto>
{
    public async ValueTask<PatientMedicationDto> Handle(GetPatientMedicationByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        PatientMedicationDto? dto = await dbContext.PatientMedications
            .AsNoTracking()
            .Where(m => m.Id == query.MedicationId)
            .Select(m => new PatientMedicationDto(
                m.Id, m.PatientId, m.DrugName, m.RxAui, m.RxCode, m.Ndc, m.Prescriber,
                m.StartDate, m.EndDate, m.DoseValue, m.DoseUnitId, m.DosePeriodValue, m.DosePeriodUnit,
                m.Instructions, m.Indication, m.IsActive,
                m.CreatedByName, m.CreatedAtUtc, m.UpdatedByName, m.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto ?? throw new NotFoundException($"Medication {query.MedicationId} not found.");
    }
}
```

`.../GetPatientMedicationById/GetPatientMedicationByIdEndpoint.cs` — GET `/medications/{id:guid}`, name `GetPatientMedicationById`, permission `Medications.View` (same shape as `GetPatientAllergyByIdEndpoint` with the medication query):

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.GetPatientMedicationById;

public static class GetPatientMedicationByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientMedicationByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/medications/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientMedicationByIdQuery(id), ct)))
            .WithName("GetPatientMedicationById")
            .WithSummary("Get a single medication-list entry")
            .RequirePermission(PatientPermissions.Medications.View);
    }
}
```

`.../SearchPatientMedications/SearchPatientMedicationsQueryHandler.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;

public sealed class SearchPatientMedicationsQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientMedicationsQuery, PagedResponse<PatientMedicationDto>>
{
    public async ValueTask<PagedResponse<PatientMedicationDto>> Handle(
        SearchPatientMedicationsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientMedication> q = dbContext.PatientMedications
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeInactive)
        {
            q = q.Where(x => x.IsActive);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientMedicationDto> items = await q
            .OrderByDescending(x => x.IsActive)
            .ThenByDescending(x => x.StartDate)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(m => new PatientMedicationDto(
                m.Id, m.PatientId, m.DrugName, m.RxAui, m.RxCode, m.Ndc, m.Prescriber,
                m.StartDate, m.EndDate, m.DoseValue, m.DoseUnitId, m.DosePeriodValue, m.DosePeriodUnit,
                m.Instructions, m.Indication, m.IsActive,
                m.CreatedByName, m.CreatedAtUtc, m.UpdatedByName, m.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientMedicationDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
```

`.../SearchPatientMedications/SearchPatientMedicationsQueryValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;

public sealed class SearchPatientMedicationsQueryValidator : AbstractValidator<SearchPatientMedicationsQuery>
{
    public SearchPatientMedicationsQueryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
```

`.../SearchPatientMedications/SearchPatientMedicationsEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;

public static class SearchPatientMedicationsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientMedicationsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/medications",
                async (
                    Guid patientId,
                    bool? includeInactive,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientMedicationsQuery(
                            patientId,
                            includeInactive ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientMedications")
            .WithSummary("Search a patient's medication list")
            .RequirePermission(PatientPermissions.Medications.View);
    }
}
```

In `PatientModule.cs` map after the allergy endpoints:

```csharp
        // Medication endpoints — literal /medications collection route before /medications/{id:guid}
        group.MapSearchPatientMedicationsEndpoint();
        group.MapCreatePatientMedicationEndpoint();
        group.MapGetPatientMedicationByIdEndpoint();
        group.MapUpdatePatientMedicationEndpoint();
```

- [ ] **Step 8: Run tests + build — expect green**

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Patient.Tests --no-restore && dotnet test src/Tests/Architecture.Tests --no-restore
```

Expected: PASS, 0 warnings.

- [ ] **Step 9: Commit**

```bash
git add src/Modules/Patient src/Tests/Patient.Tests
git commit -m "feat(patient): PatientMedication aggregate + CRUD slices with no-medications flag clearing"
```

---

### Task 3: Backend — PatientNote aggregate + slices

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Domain/PatientNote.cs`
- Create: `src/Modules/Patient/Modules.Patient/Data/Configurations/PatientNoteConfiguration.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/Dtos/PatientNoteDto.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientNotes/{CreatePatientNoteCommand,UpdatePatientNoteCommand,DeletePatientNoteCommand,SearchPatientNotesQuery}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientNotes/CreatePatientNote/{CreatePatientNoteCommandHandler,CreatePatientNoteCommandValidator,CreatePatientNoteEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientNotes/UpdatePatientNote/{UpdatePatientNoteCommandHandler,UpdatePatientNoteCommandValidator,UpdatePatientNoteEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientNotes/DeletePatientNote/{DeletePatientNoteCommandHandler,DeletePatientNoteCommandValidator,DeletePatientNoteEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientNotes/SearchPatientNotes/{SearchPatientNotesQueryHandler,SearchPatientNotesQueryValidator,SearchPatientNotesEndpoint}.cs`
- Modify: `PatientPermissions.cs` (add `Notes`), `PatientDbContext.cs`, `PatientModule.cs`
- Test: `src/Tests/Patient.Tests/Features/PatientNoteHandlerTests.cs`

**Interfaces:**
- Produces (used by Tasks 11, 12, 15):
  - `PatientNoteDto(Guid Id, Guid PatientId, string Name, string? Description, bool IsMedicalAlert, string? CreatedByName, DateTime CreatedAtUtc, string? UpdatedByName, DateTime? UpdatedAtUtc)`
  - `CreatePatientNoteCommand(Guid PatientId, string Name, string? Description, bool IsMedicalAlert) : ICommand<Guid>`
  - `UpdatePatientNoteCommand(Guid NoteId, string Name, string? Description, bool IsMedicalAlert) : ICommand<Unit>`
  - `DeletePatientNoteCommand(Guid NoteId) : ICommand<Unit>` (soft delete)
  - `SearchPatientNotesQuery(Guid PatientId, bool MedicalAlertsOnly = false, bool IncludeDeleted = false, int PageNumber = 1, int PageSize = 100) : IQuery<PagedResponse<PatientNoteDto>>`
  - Routes: `GET/POST api/v1/patient/notes`, `PUT/DELETE api/v1/patient/notes/{id:guid}`
  - Permissions: `PatientPermissions.Notes.{View,Create,Update,Delete}`

- [ ] **Step 1: Domain entity**

`src/Modules/Patient/Modules.Patient/Domain/PatientNote.cs`:

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A free-form patient chart note (legacy <c>PatientNotes</c>: <c>pnName</c>/<c>pnDescription</c>/
/// <c>pnMedicalAlert</c>/<c>pnDeleted</c>). Notes flagged <see cref="IsMedicalAlert"/> surface in the
/// chart's Medical Alerts banner alongside alert-flagged problems. Soft-deletable (legacy pnDeleted).
/// </summary>
public sealed class PatientNote : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid PatientId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsMedicalAlert { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private PatientNote() { }

    public static PatientNote Create(
        Guid patientId,
        string name,
        string? description,
        bool isMedicalAlert,
        string? createdByUserId,
        string? createdByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PatientNote
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsMedicalAlert = isMedicalAlert,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? description,
        bool isMedicalAlert,
        string? updatedByUserId,
        string? updatedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsMedicalAlert = isMedicalAlert;
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
```

- [ ] **Step 2: EF configuration + DbSet**

`.../Data/Configurations/PatientNoteConfiguration.cs`:

```csharp
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class PatientNoteConfiguration : IEntityTypeConfiguration<PatientNote>
{
    public void Configure(EntityTypeBuilder<PatientNote> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PatientNotes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(8000);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.Property(x => x.UpdatedByUserId).HasMaxLength(256);
        builder.Property(x => x.UpdatedByName).HasMaxLength(256);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.PatientId);
        builder.HasIndex(x => new { x.PatientId, x.IsDeleted });

        builder.Ignore(x => x.DomainEvents);
    }
}
```

`PatientDbContext.cs`:

```csharp
    public DbSet<Domain.PatientNote> PatientNotes => Set<Domain.PatientNote>();
```

```csharp
        modelBuilder.ApplyConfiguration(new PatientNoteConfiguration());
```

- [ ] **Step 3: Contracts**

`Modules.Patient.Contracts/Dtos/PatientNoteDto.cs`:

```csharp
namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientNoteDto(
    Guid Id,
    Guid PatientId,
    string Name,
    string? Description,
    bool IsMedicalAlert,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
```

`Modules.Patient.Contracts/v1/PatientNotes/CreatePatientNoteCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record CreatePatientNoteCommand(
    Guid PatientId,
    string Name,
    string? Description,
    bool IsMedicalAlert) : ICommand<Guid>;
```

`.../UpdatePatientNoteCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record UpdatePatientNoteCommand(
    Guid NoteId,
    string Name,
    string? Description,
    bool IsMedicalAlert) : ICommand<Unit>;
```

`.../DeletePatientNoteCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record DeletePatientNoteCommand(Guid NoteId) : ICommand<Unit>;
```

`.../SearchPatientNotesQuery.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientNotes;

public sealed record SearchPatientNotesQuery(
    Guid PatientId,
    bool MedicalAlertsOnly = false,
    bool IncludeDeleted = false,
    int PageNumber = 1,
    int PageSize = 100) : IQuery<PagedResponse<PatientNoteDto>>;
```

- [ ] **Step 4: Permissions**

In `PatientPermissions.cs`:

```csharp
    public static class Notes
    {
        public const string Resource = "Patient.Notes";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }
```

`All` additions:

```csharp
        new("View Notes",   ActionConstants.View,   Notes.Resource, IsBasic: true),
        new("Create Notes", ActionConstants.Create, Notes.Resource),
        new("Update Notes", ActionConstants.Update, Notes.Resource),
        new("Delete Notes", ActionConstants.Delete, Notes.Resource),
```

- [ ] **Step 5: Write failing handler tests**

`src/Tests/Patient.Tests/Features/PatientNoteHandlerTests.cs` — `CreateContext`/`User()` helpers copied from `PatientAllergyHandlerTests`; notes don't need a seeded patient (no flag logic):

```csharp
// usings as in PatientAllergyHandlerTests plus:
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;
using FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;
using FSH.Modules.Patient.Features.v1.PatientNotes.SearchPatientNotes;
using FSH.Modules.Patient.Features.v1.PatientNotes.UpdatePatientNote;

namespace Patient.Tests.Features;

public sealed class PatientNoteHandlerTests
{
    // CreateContext + User() copied verbatim from PatientAllergyHandlerTests.

    [Fact]
    public async Task Create_Then_Update_Then_SoftDelete_Roundtrip()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        var create = new CreatePatientNoteCommandHandler(db, User());
        var update = new UpdatePatientNoteCommandHandler(db, User());
        var delete = new DeletePatientNoteCommandHandler(db, User());

        Guid id = await create.Handle(
            new CreatePatientNoteCommand(patientId, "Fall risk", "Uses a cane", true), CancellationToken.None);

        var saved = await db.PatientNotes.FindAsync(id);
        saved!.Name.ShouldBe("Fall risk");
        saved.IsMedicalAlert.ShouldBeTrue();

        await update.Handle(new UpdatePatientNoteCommand(id, "Fall risk", "Uses a walker", false), CancellationToken.None);
        (await db.PatientNotes.FindAsync(id))!.Description.ShouldBe("Uses a walker");

        await delete.Handle(new DeletePatientNoteCommand(id), CancellationToken.None);
        (await db.PatientNotes.FindAsync(id))!.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_Should_Filter_Deleted_And_MedicalAlerts()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        var create = new CreatePatientNoteCommandHandler(db, User());
        var delete = new DeletePatientNoteCommandHandler(db, User());
        Guid keep = await create.Handle(new CreatePatientNoteCommand(patientId, "Alert note", null, true), CancellationToken.None);
        Guid gone = await create.Handle(new CreatePatientNoteCommand(patientId, "Deleted note", null, false), CancellationToken.None);
        await delete.Handle(new DeletePatientNoteCommand(gone), CancellationToken.None);
        var sut = new SearchPatientNotesQueryHandler(db);

        var visible = await sut.Handle(new SearchPatientNotesQuery(patientId), CancellationToken.None);
        var alerts = await sut.Handle(new SearchPatientNotesQuery(patientId, MedicalAlertsOnly: true), CancellationToken.None);

        visible.Items.Count.ShouldBe(1);
        visible.Items[0].Id.ShouldBe(keep);
        alerts.Items.Count.ShouldBe(1);
        alerts.Items[0].IsMedicalAlert.ShouldBeTrue();
    }
}
```

- [ ] **Step 6: Run — expect compile failure**

```bash
dotnet test src/Tests/Patient.Tests --no-restore
```

- [ ] **Step 7: Implement handlers, validators, endpoints**

`.../CreatePatientNote/CreatePatientNoteCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Context;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;

public sealed class CreatePatientNoteCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientNoteCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientNote note = PatientNote.Create(
            command.PatientId,
            command.Name,
            command.Description,
            command.IsMedicalAlert,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.PatientNotes.Add(note);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return note.Id;
    }
}
```

`.../CreatePatientNote/CreatePatientNoteCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;

public sealed class CreatePatientNoteCommandValidator : AbstractValidator<CreatePatientNoteCommand>
{
    public CreatePatientNoteCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Description).MaximumLength(8000);
    }
}
```

`.../CreatePatientNote/CreatePatientNoteEndpoint.cs` — POST `/notes`, name `CreatePatientNote`, permission `PatientPermissions.Notes.Create` (same shape as `CreatePatientAllergyEndpoint`).

`.../UpdatePatientNote/UpdatePatientNoteCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.UpdatePatientNote;

public sealed class UpdatePatientNoteCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientNoteCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientNote note = await dbContext.PatientNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Note {command.NoteId} not found.");

        note.Update(
            command.Name,
            command.Description,
            command.IsMedicalAlert,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
```

`.../UpdatePatientNote/UpdatePatientNoteCommandValidator.cs` — `NoteId` NotEmpty + same Name/Description rules as create.

`.../UpdatePatientNote/UpdatePatientNoteEndpoint.cs` — PUT `/notes/{id:guid}`, id-vs-`command.NoteId` check, name `UpdatePatientNote`, permission `Notes.Update` (same shape as `UpdatePatientAllergyEndpoint`).

`.../DeletePatientNote/DeletePatientNoteCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;

public sealed class DeletePatientNoteCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<DeletePatientNoteCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePatientNoteCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientNote note = await dbContext.PatientNotes
            .FirstOrDefaultAsync(n => n.Id == command.NoteId && !n.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Note {command.NoteId} not found.");

        note.Delete(currentUser.GetUserId().ToString());
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
```

`.../DeletePatientNote/DeletePatientNoteCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;

public sealed class DeletePatientNoteCommandValidator : AbstractValidator<DeletePatientNoteCommand>
{
    public DeletePatientNoteCommandValidator()
    {
        RuleFor(x => x.NoteId).NotEmpty();
    }
}
```

`.../DeletePatientNote/DeletePatientNoteEndpoint.cs` — DELETE `/notes/{id:guid}` sending `new DeletePatientNoteCommand(id)`, `Results.NoContent()`, name `DeletePatientNote`, permission `Notes.Delete` (mirror `DeletePatientProblemEndpoint`).

`.../SearchPatientNotes/SearchPatientNotesQueryHandler.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientNotes.SearchPatientNotes;

public sealed class SearchPatientNotesQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<SearchPatientNotesQuery, PagedResponse<PatientNoteDto>>
{
    public async ValueTask<PagedResponse<PatientNoteDto>> Handle(
        SearchPatientNotesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 100 : query.PageSize;

        IQueryable<Domain.PatientNote> q = dbContext.PatientNotes
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId);

        if (!query.IncludeDeleted)
        {
            q = q.Where(x => !x.IsDeleted);
        }

        if (query.MedicalAlertsOnly)
        {
            q = q.Where(x => x.IsMedicalAlert);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<PatientNoteDto> items = await q
            .OrderByDescending(x => x.IsMedicalAlert)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(n => new PatientNoteDto(
                n.Id, n.PatientId, n.Name, n.Description, n.IsMedicalAlert,
                n.CreatedByName, n.CreatedAtUtc, n.UpdatedByName, n.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<PatientNoteDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
```

`.../SearchPatientNotes/SearchPatientNotesQueryValidator.cs` — `PatientId` NotEmpty, `PageNumber > 0`, `PageSize` 1–200 (same as allergy search validator).

`.../SearchPatientNotes/SearchPatientNotesEndpoint.cs` — GET `/notes` with query params `(Guid patientId, bool? medicalAlertsOnly, bool? includeDeleted, int? pageNumber, int? pageSize)`, name `SearchPatientNotes`, permission `Notes.View` (same shape as `SearchPatientAllergiesEndpoint`).

`PatientModule.cs`:

```csharp
        // Note endpoints
        group.MapSearchPatientNotesEndpoint();
        group.MapCreatePatientNoteEndpoint();
        group.MapUpdatePatientNoteEndpoint();
        group.MapDeletePatientNoteEndpoint();
```

- [ ] **Step 8: Run tests + build — green**

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Patient.Tests --no-restore && dotnet test src/Tests/Architecture.Tests --no-restore
```

- [ ] **Step 9: Commit**

```bash
git add src/Modules/Patient src/Tests/Patient.Tests
git commit -m "feat(patient): PatientNote aggregate + CRUD slices with medical-alert flag"
```

---

### Task 4: Backend — MedicationReconciledDate + mark/list slices

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Domain/MedicationReconciledDate.cs`
- Create: `src/Modules/Patient/Modules.Patient/Data/Configurations/MedicationReconciledDateConfiguration.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/Dtos/MedicationReconciledDateDto.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/PatientMedications/{MarkMedicationsReconciledCommand,GetMedicationReconciledDatesQuery}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientMedications/MarkMedicationsReconciled/{MarkMedicationsReconciledCommandHandler,MarkMedicationsReconciledCommandValidator,MarkMedicationsReconciledEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/PatientMedications/GetMedicationReconciledDates/{GetMedicationReconciledDatesQueryHandler,GetMedicationReconciledDatesEndpoint}.cs`
- Modify: `PatientDbContext.cs`, `PatientModule.cs`
- Test: `src/Tests/Patient.Tests/Features/MedicationReconciliationHandlerTests.cs`

**Interfaces:**
- Produces (used by Tasks 6, 11, 12, 14):
  - `MedicationReconciledDateDto(Guid Id, Guid PatientId, DateTime ReconciledOn, string? CreatedByName, DateTime CreatedAtUtc)`
  - `MarkMedicationsReconciledCommand(Guid PatientId) : ICommand<Guid>` — records "today"
  - `GetMedicationReconciledDatesQuery(Guid PatientId) : IQuery<IReadOnlyList<MedicationReconciledDateDto>>`
  - Routes: `GET api/v1/patient/medication-reconciliations?patientId=...`, `POST api/v1/patient/medication-reconciliations`
  - Permissions: reuses `PatientPermissions.Medications.View` (GET) / `.Update` (POST)

- [ ] **Step 1: Domain + config + DbSet**

`src/Modules/Patient/Modules.Patient/Domain/MedicationReconciledDate.cs`:

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A record that a clinician reconciled the patient's medication list on a given date
/// (legacy <c>MedicationReconciledDates</c>, surfaced in the "Dates Reconciled" dialog).
/// Append-only history — no update or delete.
/// </summary>
public sealed class MedicationReconciledDate : AggregateRoot<Guid>
{
    public Guid PatientId { get; private set; }
    public DateTime ReconciledOn { get; private set; }
    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private MedicationReconciledDate() { }

    public static MedicationReconciledDate Create(
        Guid patientId,
        DateTime reconciledOn,
        string? createdByUserId,
        string? createdByName)
    {
        return new MedicationReconciledDate
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            ReconciledOn = reconciledOn.Date,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
```

`.../Data/Configurations/MedicationReconciledDateConfiguration.cs`:

```csharp
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Patient.Data.Configurations;

public sealed class MedicationReconciledDateConfiguration : IEntityTypeConfiguration<MedicationReconciledDate>
{
    public void Configure(EntityTypeBuilder<MedicationReconciledDate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MedicationReconciledDates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PatientId).IsRequired();
        builder.Property(x => x.ReconciledOn).HasColumnType("date").IsRequired();
        builder.Property(x => x.CreatedByUserId).HasMaxLength(256);
        builder.Property(x => x.CreatedByName).HasMaxLength(256);
        builder.HasIndex(x => x.PatientId);
        builder.Ignore(x => x.DomainEvents);
    }
}
```

`PatientDbContext.cs`:

```csharp
    public DbSet<Domain.MedicationReconciledDate> MedicationReconciledDates => Set<Domain.MedicationReconciledDate>();
```

```csharp
        modelBuilder.ApplyConfiguration(new MedicationReconciledDateConfiguration());
```

- [ ] **Step 2: Contracts**

`Modules.Patient.Contracts/Dtos/MedicationReconciledDateDto.cs`:

```csharp
namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record MedicationReconciledDateDto(
    Guid Id,
    Guid PatientId,
    DateTime ReconciledOn,
    string? CreatedByName,
    DateTime CreatedAtUtc);
```

`Modules.Patient.Contracts/v1/PatientMedications/MarkMedicationsReconciledCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record MarkMedicationsReconciledCommand(Guid PatientId) : ICommand<Guid>;
```

`.../GetMedicationReconciledDatesQuery.cs`:

```csharp
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record GetMedicationReconciledDatesQuery(Guid PatientId)
    : IQuery<IReadOnlyList<MedicationReconciledDateDto>>;
```

- [ ] **Step 3: Failing tests**

`src/Tests/Patient.Tests/Features/MedicationReconciliationHandlerTests.cs` (helpers copied from `PatientAllergyHandlerTests`):

```csharp
// usings as in PatientAllergyHandlerTests plus:
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Features.v1.PatientMedications.GetMedicationReconciledDates;
using FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;

namespace Patient.Tests.Features;

public sealed class MedicationReconciliationHandlerTests
{
    // CreateContext + User() copied verbatim from PatientAllergyHandlerTests.

    [Fact]
    public async Task Mark_Then_List_Returns_History_Newest_First()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        var mark = new MarkMedicationsReconciledCommandHandler(db, User());
        var list = new GetMedicationReconciledDatesQueryHandler(db);

        Guid id = await mark.Handle(new MarkMedicationsReconciledCommand(patientId), CancellationToken.None);

        var dates = await list.Handle(new GetMedicationReconciledDatesQuery(patientId), CancellationToken.None);
        dates.Count.ShouldBe(1);
        dates[0].Id.ShouldBe(id);
        dates[0].ReconciledOn.Date.ShouldBe(DateTime.UtcNow.Date);
    }
}
```

- [ ] **Step 4: Run — expect compile failure**

```bash
dotnet test src/Tests/Patient.Tests --no-restore
```

- [ ] **Step 5: Implement**

`.../MarkMedicationsReconciled/MarkMedicationsReconciledCommandHandler.cs`:

```csharp
using FSH.Framework.Core.Context;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;

public sealed class MarkMedicationsReconciledCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<MarkMedicationsReconciledCommand, Guid>
{
    public async ValueTask<Guid> Handle(MarkMedicationsReconciledCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        MedicationReconciledDate entry = MedicationReconciledDate.Create(
            command.PatientId,
            DateTime.UtcNow,
            currentUser.GetUserId().ToString(),
            currentUser.Name);

        dbContext.MedicationReconciledDates.Add(entry);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entry.Id;
    }
}
```

`.../MarkMedicationsReconciled/MarkMedicationsReconciledCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;

public sealed class MarkMedicationsReconciledCommandValidator : AbstractValidator<MarkMedicationsReconciledCommand>
{
    public MarkMedicationsReconciledCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
```

`.../MarkMedicationsReconciled/MarkMedicationsReconciledEndpoint.cs` — POST `/medication-reconciliations` with body command, name `MarkMedicationsReconciled`, permission `PatientPermissions.Medications.Update`.

`.../GetMedicationReconciledDates/GetMedicationReconciledDatesQueryHandler.cs`:

```csharp
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.GetMedicationReconciledDates;

public sealed class GetMedicationReconciledDatesQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetMedicationReconciledDatesQuery, IReadOnlyList<MedicationReconciledDateDto>>
{
    public async ValueTask<IReadOnlyList<MedicationReconciledDateDto>> Handle(
        GetMedicationReconciledDatesQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return await dbContext.MedicationReconciledDates
            .AsNoTracking()
            .Where(x => x.PatientId == query.PatientId)
            .OrderByDescending(x => x.ReconciledOn)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new MedicationReconciledDateDto(
                x.Id, x.PatientId, x.ReconciledOn, x.CreatedByName, x.CreatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
```

`.../GetMedicationReconciledDates/GetMedicationReconciledDatesEndpoint.cs` — GET `/medication-reconciliations` with `(Guid patientId, ...)`, name `GetMedicationReconciledDates`, permission `Medications.View`.

`PatientModule.cs` (register the literal route before `/medications/{id:guid}` is not required since the segment differs, but keep them grouped after the medication endpoints):

```csharp
        // Medication reconciliation
        group.MapGetMedicationReconciledDatesEndpoint();
        group.MapMarkMedicationsReconciledEndpoint();
```

- [ ] **Step 6: Run + build green, commit**

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Patient.Tests --no-restore
git add src/Modules/Patient src/Tests/Patient.Tests
git commit -m "feat(patient): medication reconciliation dates (mark + history)"
```

---

### Task 5: Backend — Set No-Known-Allergies / No-Known-Medications commands with legacy 409 rules

**Files:**
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/SetPatientNoKnownAllergiesCommand.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/SetPatientNoKnownMedicationsCommand.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/SetPatientNoKnownAllergies/{SetPatientNoKnownAllergiesCommandHandler,SetPatientNoKnownAllergiesCommandValidator,SetPatientNoKnownAllergiesEndpoint}.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/SetPatientNoKnownMedications/{SetPatientNoKnownMedicationsCommandHandler,SetPatientNoKnownMedicationsCommandValidator,SetPatientNoKnownMedicationsEndpoint}.cs`
- Modify: `PatientModule.cs`
- Test: `src/Tests/Patient.Tests/Features/SetNoKnownFlagsHandlerTests.cs`

**Interfaces:**
- Consumes: `Patient.SetNoKnownAllergies/SetNoKnownMedications` (Task 1), `PatientAllergies`/`PatientMedications` DbSets (Tasks 1–2).
- Produces (used by Tasks 12–14):
  - `SetPatientNoKnownAllergiesCommand(Guid PatientId, bool Value) : ICommand<Unit>` → `PUT api/v1/patient/patients/{id:guid}/no-known-allergies`
  - `SetPatientNoKnownMedicationsCommand(Guid PatientId, bool Value) : ICommand<Unit>` → `PUT api/v1/patient/patients/{id:guid}/no-known-medications`
  - 409 Conflict with exact legacy messages when setting `true` while active rows exist.
  - Permission: `PatientPermissions.Patients.Update` for both.

- [ ] **Step 1: Contracts**

`.../v1/Patients/SetPatientNoKnownAllergiesCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record SetPatientNoKnownAllergiesCommand(Guid PatientId, bool Value) : ICommand<Unit>;
```

`.../v1/Patients/SetPatientNoKnownMedicationsCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record SetPatientNoKnownMedicationsCommand(Guid PatientId, bool Value) : ICommand<Unit>;
```

- [ ] **Step 2: Failing tests**

`src/Tests/Patient.Tests/Features/SetNoKnownFlagsHandlerTests.cs` (helpers from `PatientAllergyHandlerTests`, including `SeedPatientAsync`):

```csharp
// usings as in PatientAllergyHandlerTests plus:
using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;
using FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownMedications;

namespace Patient.Tests.Features;

public sealed class SetNoKnownFlagsHandlerTests
{
    // CreateContext + User() + SeedPatientAsync copied from PatientAllergyHandlerTests.

    [Fact]
    public async Task SetNoAllergies_True_Throws_Conflict_When_Active_Allergy_Exists()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        db.PatientAllergies.Add(PatientAllergy.Create(patientId, "Penicillin", null, null, null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), true, null, null));
        await db.SaveChangesAsync();
        var sut = new SetPatientNoKnownAllergiesCommandHandler(db);

        var ex = await Should.ThrowAsync<CustomException>(() =>
            sut.Handle(new SetPatientNoKnownAllergiesCommand(patientId, true), CancellationToken.None).AsTask());

        ex.Message.ShouldBe("You cannot set No Allergies when Active allergies exist.");
        ex.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SetNoAllergies_True_Succeeds_When_Only_Inactive_Exist()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        db.PatientAllergies.Add(PatientAllergy.Create(patientId, "Latex", null, null, null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false, null, null));
        await db.SaveChangesAsync();
        var sut = new SetPatientNoKnownAllergiesCommandHandler(db);

        await sut.Handle(new SetPatientNoKnownAllergiesCommand(patientId, true), CancellationToken.None);

        (await db.Patients.FindAsync(patientId))!.HasNoKnownAllergies.ShouldBeTrue();
    }

    [Fact]
    public async Task SetNoMedications_True_Throws_Conflict_When_Active_Medication_Exists()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        db.PatientMedications.Add(PatientMedication.Create(patientId, "Lisinopril", null, null, null, null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, null, null, null,
            true, null, null));
        await db.SaveChangesAsync();
        var sut = new SetPatientNoKnownMedicationsCommandHandler(db);

        var ex = await Should.ThrowAsync<CustomException>(() =>
            sut.Handle(new SetPatientNoKnownMedicationsCommand(patientId, true), CancellationToken.None).AsTask());

        ex.Message.ShouldBe("You cannot set No Medications when Active medications exist.");
    }

    [Fact]
    public async Task Set_False_Always_Succeeds()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db, noKnownAllergies: true);
        var sut = new SetPatientNoKnownAllergiesCommandHandler(db);

        await sut.Handle(new SetPatientNoKnownAllergiesCommand(patientId, false), CancellationToken.None);

        (await db.Patients.FindAsync(patientId))!.HasNoKnownAllergies.ShouldBeFalse();
    }
}
```

> `CustomException.StatusCode` property name: verify against `src/BuildingBlocks/Core/Exceptions/CustomException.cs` before running (adjust the assertion to the actual property).

- [ ] **Step 3: Run — expect compile failure**

```bash
dotnet test src/Tests/Patient.Tests --no-restore
```

- [ ] **Step 4: Implement**

`.../SetPatientNoKnownAllergies/SetPatientNoKnownAllergiesCommandHandler.cs`:

```csharp
using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;

public sealed class SetPatientNoKnownAllergiesCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<SetPatientNoKnownAllergiesCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetPatientNoKnownAllergiesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        if (command.Value)
        {
            bool hasActive = await dbContext.PatientAllergies
                .AnyAsync(a => a.PatientId == command.PatientId && a.IsActive, cancellationToken)
                .ConfigureAwait(false);
            if (hasActive)
            {
                throw new CustomException(
                    "You cannot set No Allergies when Active allergies exist.",
                    (IEnumerable<string>?)null,
                    HttpStatusCode.Conflict);
            }
        }

        patient.SetNoKnownAllergies(command.Value);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
```

`.../SetPatientNoKnownAllergies/SetPatientNoKnownAllergiesCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.Patients;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;

public sealed class SetPatientNoKnownAllergiesCommandValidator : AbstractValidator<SetPatientNoKnownAllergiesCommand>
{
    public SetPatientNoKnownAllergiesCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
```

`.../SetPatientNoKnownAllergies/SetPatientNoKnownAllergiesEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;

public static class SetPatientNoKnownAllergiesEndpoint
{
    public sealed record SetFlagRequest(bool Value);

    internal static RouteHandlerBuilder MapSetPatientNoKnownAllergiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/patients/{id:guid}/no-known-allergies",
                async (Guid id, SetFlagRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SetPatientNoKnownAllergiesCommand(id, request.Value), ct);
                    return Results.NoContent();
                })
            .WithName("SetPatientNoKnownAllergies")
            .WithSummary("Set or clear the patient's No Known Allergies flag")
            .RequirePermission(PatientPermissions.Patients.Update);
    }
}
```

`SetPatientNoKnownMedications` mirror — handler identical with `PatientMedications` + message `"You cannot set No Medications when Active medications exist."` + `patient.SetNoKnownMedications(command.Value)`; validator identical; endpoint at `/patients/{id:guid}/no-known-medications`, name `SetPatientNoKnownMedications`:

```csharp
using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownMedications;

public sealed class SetPatientNoKnownMedicationsCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<SetPatientNoKnownMedicationsCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetPatientNoKnownMedicationsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        if (command.Value)
        {
            bool hasActive = await dbContext.PatientMedications
                .AnyAsync(m => m.PatientId == command.PatientId && m.IsActive, cancellationToken)
                .ConfigureAwait(false);
            if (hasActive)
            {
                throw new CustomException(
                    "You cannot set No Medications when Active medications exist.",
                    (IEnumerable<string>?)null,
                    HttpStatusCode.Conflict);
            }
        }

        patient.SetNoKnownMedications(command.Value);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
```

`PatientModule.cs` — register these BEFORE `MapGetPatientByIdEndpoint()` (literal sub-segment under `/patients/{id}`):

```csharp
        group.MapSetPatientNoKnownAllergiesEndpoint();
        group.MapSetPatientNoKnownMedicationsEndpoint();
```

- [ ] **Step 5: Run + build green, commit**

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Patient.Tests --no-restore && dotnet test src/Tests/Architecture.Tests --no-restore
git add src/Modules/Patient src/Tests/Patient.Tests
git commit -m "feat(patient): set no-known-allergies/medications commands with legacy conflict rules"
```

---

### Task 6: Patient module EF migration

**Files:**
- Create (generated): `src/Host/FSH.Starter.Migrations.PostgreSQL/Patient/<timestamp>_AddPatientClinicalLists.cs` + `.Designer.cs` (+ snapshot update)

- [ ] **Step 1: Build, then generate**

```bash
dotnet build src/FSH.Starter.slnx
dotnet ef migrations add AddPatientClinicalLists \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context PatientDbContext \
  --output-dir Patient
```

Expected: migration creating `patient.PatientAllergies`, `patient.PatientMedications`, `patient.PatientNotes`, `patient.MedicationReconciledDates` with the indexes from the configurations. There must be NO changes to the `Patients` table (the flags already exist).

- [ ] **Step 2: Review SQL**

```bash
dotnet ef migrations script --idempotent \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context PatientDbContext
```

Check: only CREATE TABLE/INDEX statements for the four new tables; no drops.

- [ ] **Step 3: Apply locally + verify**

```bash
dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply
dotnet run --project src/Host/FSH.Starter.DbMigrator -- list-pending
```

Expected: apply succeeds; list-pending shows none for PatientDbContext. (Local Postgres: localhost:5432, db `fsh` — pass `DatabaseOptions__ConnectionString` env var if needed, DbMigrator has no appsettings.)

- [ ] **Step 4: Commit**

```bash
git add src/Host/FSH.Starter.Migrations.PostgreSQL
git commit -m "feat(patient): EF migration AddPatientClinicalLists (allergies, medications, notes, reconciled dates)"
```

---

### Task 7: Administration — Drug catalog entity + list/CRUD slices

**Files:**
- Create: `src/Modules/Administration/Modules.Administration/Domain/Drug.cs`
- Create: `src/Modules/Administration/Modules.Administration/Data/Configurations/DrugConfiguration.cs`
- Create: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/DrugDto.cs`
- Create: `src/Modules/Administration/Modules.Administration.Contracts/v1/Drugs/{ListDrugsQuery,GetDrugByIdQuery,CreateDrugCommand,UpdateDrugCommand,DeleteDrugCommand}.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Drugs/ListDrugs/{ListDrugsQueryHandler,ListDrugsQueryValidator,ListDrugsEndpoint}.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Drugs/GetDrugById/{GetDrugByIdQueryHandler,GetDrugByIdEndpoint}.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Drugs/CreateDrug/{CreateDrugCommandHandler,CreateDrugCommandValidator,CreateDrugEndpoint}.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Drugs/UpdateDrug/{UpdateDrugCommandHandler,UpdateDrugCommandValidator,UpdateDrugEndpoint}.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Drugs/DeleteDrug/{DeleteDrugCommandHandler,DeleteDrugCommandValidator,DeleteDrugEndpoint}.cs`
- Modify: `src/Modules/Administration/Modules.Administration.Contracts/Authorization/AdministrationPermissions.cs` (add `Drugs` + `All` entries)
- Modify: `src/Modules/Administration/Modules.Administration/Data/AdministrationDbContext.cs` (DbSet; check whether this context uses `ApplyConfigurationsFromAssembly` — if so the config is picked up automatically, otherwise add `ApplyConfiguration`)
- Modify: `src/Modules/Administration/Modules.Administration/AdministrationModule.cs` (map endpoints)
- Test: `src/Tests/Administration.Tests/Features/DrugHandlerTests.cs`

**Interfaces:**
- Produces (used by Tasks 8–10, 13–14, 16):
  - `DrugDto(int Id, string Name, string? RxAui, string? RxCui, string? Tty, string? Sab, string? Code, bool IsActive, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc)`
  - `ListDrugsQuery(string? Search = null, bool? IsActive = null, int PageNumber = 1, int PageSize = 20) : IQuery<PagedResponse<DrugDto>>`
  - `CreateDrugCommand(string Name, string? RxAui = null, string? RxCui = null, string? Tty = null, string? Sab = null, string? Code = null) : ICommand<int>`
  - `UpdateDrugCommand(int Id, string Name, string? RxAui, string? RxCui, string? Tty, string? Sab, string? Code, bool IsActive) : ICommand<Unit>`
  - `DeleteDrugCommand(int Id) : ICommand<Unit>` (soft delete)
  - `Drug.Create(name, rxAui, rxCui, tty, sab, code)` / `Drug.Update(...)` / `Drug.Delete(deletedBy)`
  - Routes: `GET/POST api/v1/administration/drugs`, `GET/PUT/DELETE api/v1/administration/drugs/{id:int}`
  - Permissions: `AdministrationPermissions.Drugs.{View,Create,Update,Delete}` — register View with `IsBasic: true` (chart drug picker is used by regular tenant staff, same as Diagnostics View).

- [ ] **Step 1: Domain entity (mirror `Diagnostic`)**

`src/Modules/Administration/Modules.Administration/Domain/Drug.cs`:

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A drug concept in the global catalog (legacy RxNorm <c>RXNCONSO</c> subset: RXAUI atom id,
/// RXCUI concept id, STR display name, TTY term type, SAB source vocabulary, CODE source code).
/// Cross-tenant reference data (<see cref="IGlobalEntity"/>) searched by the patient chart's
/// allergy/medication drug pickers; managed via admin CRUD and on-demand RxNav import.
/// Soft-deleted so an in-use drug can be hidden without breaking historic references.
/// </summary>
public sealed class Drug : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Name { get; private set; } = default!;
    public string? RxAui { get; private set; }
    public string? RxCui { get; private set; }
    public string? Tty { get; private set; }
    public string? Sab { get; private set; }
    public string? Code { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private Drug() { }

    public static Drug Create(
        string name,
        string? rxAui = null,
        string? rxCui = null,
        string? tty = null,
        string? sab = null,
        string? code = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Drug
        {
            Name = name.Trim(),
            RxAui = Clean(rxAui),
            RxCui = Clean(rxCui),
            Tty = Clean(tty),
            Sab = Clean(sab),
            Code = Clean(code),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? rxAui,
        string? rxCui,
        string? tty,
        string? sab,
        string? code,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        RxAui = Clean(rxAui);
        RxCui = Clean(rxCui);
        Tty = Clean(tty);
        Sab = Clean(sab);
        Code = Clean(code);
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
```

- [ ] **Step 2: EF configuration + DbSet**

`.../Data/Configurations/DrugConfiguration.cs` (mirror `DiagnosticConfiguration`'s table/index style — open it first and match its `ToTable` naming convention exactly):

```csharp
using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class DrugConfiguration : IEntityTypeConfiguration<Drug>
{
    public void Configure(EntityTypeBuilder<Drug> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Drugs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(2048).IsRequired();
        builder.Property(x => x.RxAui).HasMaxLength(12);
        builder.Property(x => x.RxCui).HasMaxLength(12);
        builder.Property(x => x.Tty).HasMaxLength(20);
        builder.Property(x => x.Sab).HasMaxLength(40);
        builder.Property(x => x.Code).HasMaxLength(64);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.HasIndex(x => x.Name);
        builder.HasIndex(x => x.RxAui).IsUnique().HasFilter("\"RxAui\" IS NOT NULL");
        builder.HasIndex(x => x.RxCui);
        builder.HasIndex(x => x.IsDeleted);

        builder.Ignore(x => x.DomainEvents);
    }
}
```

`AdministrationDbContext.cs`:

```csharp
    public DbSet<Drug> Drugs => Set<Drug>();
```

(+ `ApplyConfiguration(new DrugConfiguration())` in `OnModelCreating` only if the context registers configurations explicitly — match how `DiagnosticConfiguration` is registered.)

- [ ] **Step 3: Contracts**

`Modules.Administration.Contracts/Dtos/DrugDto.cs`:

```csharp
namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record DrugDto(
    int Id,
    string Name,
    string? RxAui,
    string? RxCui,
    string? Tty,
    string? Sab,
    string? Code,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
```

`Modules.Administration.Contracts/v1/Drugs/ListDrugsQuery.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record ListDrugsQuery(
    string? Search = null,
    bool? IsActive = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<PagedResponse<DrugDto>>;
```

`.../GetDrugByIdQuery.cs`:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record GetDrugByIdQuery(int Id) : IQuery<DrugDto>;
```

`.../CreateDrugCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record CreateDrugCommand(
    string Name,
    string? RxAui = null,
    string? RxCui = null,
    string? Tty = null,
    string? Sab = null,
    string? Code = null) : ICommand<int>;
```

`.../UpdateDrugCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record UpdateDrugCommand(
    int Id,
    string Name,
    string? RxAui,
    string? RxCui,
    string? Tty,
    string? Sab,
    string? Code,
    bool IsActive) : ICommand<Unit>;
```

`.../DeleteDrugCommand.cs`:

```csharp
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record DeleteDrugCommand(int Id) : ICommand<Unit>;
```

- [ ] **Step 4: Permissions**

In `AdministrationPermissions.cs` add:

```csharp
    public static class Drugs
    {
        public const string Resource = "Administration.Drugs";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }
```

and to its `All` list (match the file's existing entry style; give View `IsBasic: true` if and only if `Diagnostics` View has it — mirror exactly so chart users can search drugs like they search diagnostics):

```csharp
        new("View Drugs",   ActionConstants.View,   Drugs.Resource, IsBasic: true),
        new("Create Drugs", ActionConstants.Create, Drugs.Resource),
        new("Update Drugs", ActionConstants.Update, Drugs.Resource),
        new("Delete Drugs", ActionConstants.Delete, Drugs.Resource),
```

- [ ] **Step 5: Failing tests**

`src/Tests/Administration.Tests/Features/DrugHandlerTests.cs` — copy the `CreateContext` helper style from an existing Administration.Tests handler test (find one with `ls src/Tests/Administration.Tests/Features`; e.g. a Diagnostics or Clinics handler test builds `AdministrationDbContext` with in-memory options + tenant accessor substitute):

```csharp
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;
using FSH.Modules.Administration.Features.v1.Drugs.DeleteDrug;
using FSH.Modules.Administration.Features.v1.Drugs.ListDrugs;
using FSH.Modules.Administration.Features.v1.Drugs.UpdateDrug;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class DrugHandlerTests
{
    // CreateContext(dbName) — copied from the existing Administration handler-test helper.

    [Fact]
    public async Task Create_List_Update_Delete_Roundtrip()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var create = new CreateDrugCommandHandler(db);
        var list = new ListDrugsQueryHandler(db);
        var update = new UpdateDrugCommandHandler(db);
        var delete = new DeleteDrugCommandHandler(db);

        int id = await create.Handle(
            new CreateDrugCommand("Lisinopril 10 MG Oral Tablet", "1998001", "314076", "SCD", "RXNORM", "314076"),
            CancellationToken.None);

        var page = await list.Handle(new ListDrugsQuery(Search: "lisinopril"), CancellationToken.None);
        page.Items.Count.ShouldBe(1);
        page.Items[0].RxCui.ShouldBe("314076");

        await update.Handle(new UpdateDrugCommand(id, "Lisinopril 10 MG Oral Tablet", "1998001", "314076",
            "SCD", "RXNORM", "314076", IsActive: false), CancellationToken.None);
        (await list.Handle(new ListDrugsQuery(IsActive: false), CancellationToken.None)).Items.Count.ShouldBe(1);

        await delete.Handle(new DeleteDrugCommand(id), CancellationToken.None);
        (await list.Handle(new ListDrugsQuery(), CancellationToken.None)).Items.Count.ShouldBe(0);
    }
}
```

> If Administration handlers take `ICurrentUser` for delete (check `DeleteDiagnosticCommandHandler`'s constructor), match that constructor in the test and handler below. The in-memory provider does not translate `EF.Functions.ILike` — if the List test fails on that, split the search assertion into an integration-only concern and assert on unfiltered listing instead (check how existing Diagnostics tests handle it; mirror them).

- [ ] **Step 6: Run — expect compile failure**

```bash
dotnet test src/Tests/Administration.Tests --no-restore
```

- [ ] **Step 7: Implement slices (mirror the Diagnostics slices)**

`.../ListDrugs/ListDrugsQueryHandler.cs`:

```csharp
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Drugs.ListDrugs;

public sealed class ListDrugsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListDrugsQuery, PagedResponse<DrugDto>>
{
    public async ValueTask<PagedResponse<DrugDto>> Handle(ListDrugsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        int page = query.PageNumber < 1 ? 1 : query.PageNumber;
        int size = query.PageSize is < 1 or > 200 ? 20 : query.PageSize;

        IQueryable<Domain.Drug> q = dbContext.Drugs.AsNoTracking().Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string term = query.Search.Trim();
            q = q.Where(d =>
                EF.Functions.ILike(d.Name, $"%{term}%") ||
                (d.RxCui != null && EF.Functions.ILike(d.RxCui, $"{term}%")));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(d => d.IsActive == query.IsActive.Value);
        }

        long total = await q.LongCountAsync(cancellationToken).ConfigureAwait(false);

        List<DrugDto> items = await q
            .OrderBy(d => d.Name)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(d => new DrugDto(
                d.Id, d.Name, d.RxAui, d.RxCui, d.Tty, d.Sab, d.Code,
                d.IsActive, d.CreatedAtUtc, d.UpdatedAtUtc))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResponse<DrugDto>
        {
            Items = items,
            PageNumber = page,
            PageSize = size,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)size)
        };
    }
}
```

> In-memory-test caveat: if `EF.Functions.ILike` breaks the unit test (InMemory provider), mirror whatever the existing `ListDiagnosticsQueryHandler` tests do — they use the same ILike, so copy their approach for testing search.

`.../ListDrugs/ListDrugsQueryValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.ListDrugs;

public sealed class ListDrugsQueryValidator : AbstractValidator<ListDrugsQuery>
{
    public ListDrugsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.Search).MaximumLength(256);
    }
}
```

`.../ListDrugs/ListDrugsEndpoint.cs` — GET `/drugs` with `(string? search, bool? isActive, int? pageNumber, int? pageSize)`, name `ListDrugs`, summary "Search and list the drug catalog", permission `AdministrationPermissions.Drugs.View` (mirror `ListDiagnosticsEndpoint`).

`.../GetDrugById/GetDrugByIdQueryHandler.cs` — mirror `GetDiagnosticByIdQueryHandler`: select `DrugDto` by id where `!IsDeleted`, throw `NotFoundException($"Drug {query.Id} not found.")`. Endpoint GET `/drugs/{id:int}`, permission `Drugs.View`, name `GetDrugById`.

`.../CreateDrug/CreateDrugCommandHandler.cs`:

```csharp
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;

public sealed class CreateDrugCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateDrugCommand, int>
{
    public async ValueTask<int> Handle(CreateDrugCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Drug drug = Drug.Create(command.Name, command.RxAui, command.RxCui, command.Tty, command.Sab, command.Code);
        dbContext.Drugs.Add(drug);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return drug.Id;
    }
}
```

`.../CreateDrug/CreateDrugCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;

public sealed class CreateDrugCommandValidator : AbstractValidator<CreateDrugCommand>
{
    public CreateDrugCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(2048);
        RuleFor(x => x.RxAui).MaximumLength(12);
        RuleFor(x => x.RxCui).MaximumLength(12);
        RuleFor(x => x.Tty).MaximumLength(20);
        RuleFor(x => x.Sab).MaximumLength(40);
        RuleFor(x => x.Code).MaximumLength(64);
    }
}
```

`.../CreateDrug/CreateDrugEndpoint.cs` — POST `/drugs`, name `CreateDrug`, permission `Drugs.Create`.

`.../UpdateDrug/UpdateDrugCommandHandler.cs` — load by id where `!IsDeleted` (NotFound otherwise), call `drug.Update(command.Name, command.RxAui, command.RxCui, command.Tty, command.Sab, command.Code, command.IsActive)`, save, return `Unit.Value`. Validator: same field rules as create + `Id > 0`. Endpoint PUT `/drugs/{id:int}` with id/body check, name `UpdateDrug`, permission `Drugs.Update`.

`.../DeleteDrug/DeleteDrugCommandHandler.cs` — load, `drug.Delete(currentUser...)` if the Diagnostics delete handler passes a user, else `drug.Delete(null)` — mirror `DeleteDiagnosticCommandHandler` exactly. Validator `Id > 0`. Endpoint DELETE `/drugs/{id:int}`, name `DeleteDrug`, permission `Drugs.Delete`.

In `AdministrationModule.cs` `MapEndpoints`, register with the other lookup endpoints:

```csharp
        group.MapListDrugsEndpoint();
        group.MapGetDrugByIdEndpoint();
        group.MapCreateDrugEndpoint();
        group.MapUpdateDrugEndpoint();
        group.MapDeleteDrugEndpoint();
```

- [ ] **Step 8: Run + build green, commit**

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Administration.Tests --no-restore && dotnet test src/Tests/Architecture.Tests --no-restore
git add src/Modules/Administration src/Tests/Administration.Tests
git commit -m "feat(administration): Drug catalog entity + list/CRUD slices"
```

---

### Task 8: Administration — AllergyReaction + MedicationDoseUnit lookups (with seeds)

**Files:**
- Create: `src/Modules/Administration/Modules.Administration/Domain/AllergyReaction.cs`
- Create: `src/Modules/Administration/Modules.Administration/Domain/MedicationDoseUnit.cs`
- Create: `src/Modules/Administration/Modules.Administration/Data/Configurations/{AllergyReactionConfiguration,MedicationDoseUnitConfiguration}.cs`
- Create: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/{AllergyReactionDto,MedicationDoseUnitDto}.cs`
- Create: contracts + slices for both lookups, mirroring the SmokingStatuses slice set exactly:
  - `Modules.Administration.Contracts/v1/AllergyReactions/{ListAllergyReactionsQuery,GetAllergyReactionByIdQuery,CreateAllergyReactionCommand,UpdateAllergyReactionCommand,DeleteAllergyReactionCommand}.cs`
  - `Modules.Administration/Features/v1/AllergyReactions/{ListAllergyReactions,GetAllergyReactionById,CreateAllergyReaction,UpdateAllergyReaction,DeleteAllergyReaction}/...` (handler + validator + endpoint each; List and GetById have no validator only if the SmokingStatuses ones don't — mirror)
  - same again under `v1/MedicationDoseUnits/...`
- Modify: `AdministrationPermissions.cs`, `AdministrationDbContext.cs`, `AdministrationModule.cs`
- Test: `src/Tests/Administration.Tests/Features/{AllergyReactionHandlerTests,MedicationDoseUnitHandlerTests}.cs`

**Interfaces:**
- Produces (used by Tasks 9–10, 13–14, 16):
  - `AllergyReactionDto(int Id, string Term, string? SnomedCode, bool IsActive)`
  - `MedicationDoseUnitDto(int Id, string Name, bool IsActive)`
  - `ListAllergyReactionsQuery(bool? IsActive = null) : IQuery<IReadOnlyList<AllergyReactionDto>>`
  - `ListMedicationDoseUnitsQuery(bool? IsActive = null) : IQuery<IReadOnlyList<MedicationDoseUnitDto>>`
  - Create/Update/Delete commands per lookup (`CreateAllergyReactionCommand(string Term, string? SnomedCode) : ICommand<int>`, `UpdateAllergyReactionCommand(int Id, string Term, string? SnomedCode, bool IsActive) : ICommand<Unit>`, `DeleteAllergyReactionCommand(int Id) : ICommand<Unit>`; `CreateMedicationDoseUnitCommand(string Name) : ICommand<int>`, `UpdateMedicationDoseUnitCommand(int Id, string Name, bool IsActive) : ICommand<Unit>`, `DeleteMedicationDoseUnitCommand(int Id) : ICommand<Unit>`)
  - Routes: `api/v1/administration/allergy-reactions[...]`, `api/v1/administration/medication-dose-units[...]`
  - Permissions: `AdministrationPermissions.AllergyReactions.*`, `AdministrationPermissions.MedicationDoseUnits.*` (View `IsBasic: true` — the pickers are used from the chart).

- [ ] **Step 1: Domain entities**

`Domain/AllergyReaction.cs` (mirror `SmokingStatus` — it already has the Name+SnomedCode shape):

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A curated allergy reaction option (legacy <c>SnomedAssociations</c> rows flagged
/// <c>saIsReaction</c>, joined to the SNOMED description for code + term). The patient chart's
/// allergy dialog multi-picks from this list and appends terms into the allergy's Reaction text.
/// </summary>
public sealed class AllergyReaction : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Term { get; private set; } = default!;
    public string? SnomedCode { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private AllergyReaction() { }

    public static AllergyReaction Create(string term, string? snomedCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        return new AllergyReaction { Term = term.Trim(), SnomedCode = snomedCode?.Trim(), IsActive = true };
    }

    public void Update(string term, bool isActive, string? snomedCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(term);
        Term = term.Trim();
        IsActive = isActive;
        SnomedCode = snomedCode?.Trim();
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
```

`Domain/MedicationDoseUnit.cs` — identical shape with `Name` only:

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.Administration.Domain;

/// <summary>
/// A medication dose unit (legacy <c>MedicationUnitTypes</c>, e.g. mg, mL, tablet) used by the
/// medication dialog's dose-unit dropdown.
/// </summary>
public sealed class MedicationDoseUnit : AggregateRoot<int>, ISoftDeletable, IGlobalEntity
{
    public string Name { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private MedicationDoseUnit() { }

    public static MedicationDoseUnit Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new MedicationDoseUnit { Name = name.Trim(), IsActive = true };
    }

    public void Update(string name, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        IsActive = isActive;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
```

- [ ] **Step 2: Configurations with placeholder seeds**

Mirror `SmokingStatusConfiguration` (it uses `HasData`; keep the same table-name style). Seeds are placeholders replaced by the Task 10 migration verb — same approach the six existing lookups used.

`AllergyReactionConfiguration.cs`:

```csharp
using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class AllergyReactionConfiguration : IEntityTypeConfiguration<AllergyReaction>
{
    public void Configure(EntityTypeBuilder<AllergyReaction> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AllergyReactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Term).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SnomedCode).HasMaxLength(32);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Ignore(x => x.DomainEvents);

        builder.HasData(
            new { Id = 1, Term = "Rash", SnomedCode = (string?)"271807003", IsActive = true, IsDeleted = false },
            new { Id = 2, Term = "Hives", SnomedCode = (string?)"126485001", IsActive = true, IsDeleted = false },
            new { Id = 3, Term = "Anaphylaxis", SnomedCode = (string?)"39579001", IsActive = true, IsDeleted = false },
            new { Id = 4, Term = "Nausea", SnomedCode = (string?)"422587007", IsActive = true, IsDeleted = false },
            new { Id = 5, Term = "Vomiting", SnomedCode = (string?)"422400008", IsActive = true, IsDeleted = false },
            new { Id = 6, Term = "Swelling", SnomedCode = (string?)"65124004", IsActive = true, IsDeleted = false },
            new { Id = 7, Term = "Itching", SnomedCode = (string?)"418290006", IsActive = true, IsDeleted = false },
            new { Id = 8, Term = "Shortness of breath", SnomedCode = (string?)"267036007", IsActive = true, IsDeleted = false },
            new { Id = 9, Term = "Diarrhea", SnomedCode = (string?)"62315008", IsActive = true, IsDeleted = false },
            new { Id = 10, Term = "Cough", SnomedCode = (string?)"49727002", IsActive = true, IsDeleted = false });
    }
}
```

> Match the anonymous-object vs entity-instance `HasData` style used in `SmokingStatusConfiguration` — open it and copy its exact seeding idiom (anonymous objects shown here work with private setters).

`MedicationDoseUnitConfiguration.cs`:

```csharp
using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Administration.Data.Configurations;

public sealed class MedicationDoseUnitConfiguration : IEntityTypeConfiguration<MedicationDoseUnit>
{
    public void Configure(EntityTypeBuilder<MedicationDoseUnit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MedicationDoseUnits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
        builder.Ignore(x => x.DomainEvents);

        builder.HasData(
            new { Id = 1, Name = "mg", IsActive = true, IsDeleted = false },
            new { Id = 2, Name = "mcg", IsActive = true, IsDeleted = false },
            new { Id = 3, Name = "g", IsActive = true, IsDeleted = false },
            new { Id = 4, Name = "mL", IsActive = true, IsDeleted = false },
            new { Id = 5, Name = "tablet", IsActive = true, IsDeleted = false },
            new { Id = 6, Name = "capsule", IsActive = true, IsDeleted = false },
            new { Id = 7, Name = "unit", IsActive = true, IsDeleted = false },
            new { Id = 8, Name = "puff", IsActive = true, IsDeleted = false },
            new { Id = 9, Name = "drop", IsActive = true, IsDeleted = false });
    }
}
```

`AdministrationDbContext.cs`:

```csharp
    public DbSet<AllergyReaction> AllergyReactions => Set<AllergyReaction>();
    public DbSet<MedicationDoseUnit> MedicationDoseUnits => Set<MedicationDoseUnit>();
```

- [ ] **Step 3: Contracts + slices (mirror SmokingStatuses byte-for-byte)**

Open the five SmokingStatuses slice folders and replicate for each lookup. Exact contracts:

`Contracts/Dtos/AllergyReactionDto.cs`:

```csharp
namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record AllergyReactionDto(int Id, string Term, string? SnomedCode, bool IsActive);
```

`Contracts/Dtos/MedicationDoseUnitDto.cs`:

```csharp
namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record MedicationDoseUnitDto(int Id, string Name, bool IsActive);
```

`Contracts/v1/AllergyReactions/ListAllergyReactionsQuery.cs`:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AllergyReactions;

public sealed record ListAllergyReactionsQuery(bool? IsActive = null)
    : IQuery<IReadOnlyList<AllergyReactionDto>>;
```

(`GetAllergyReactionByIdQuery(int Id) : IQuery<AllergyReactionDto>`, `CreateAllergyReactionCommand(string Term, string? SnomedCode = null) : ICommand<int>`, `UpdateAllergyReactionCommand(int Id, string Term, string? SnomedCode, bool IsActive) : ICommand<Unit>`, `DeleteAllergyReactionCommand(int Id) : ICommand<Unit>` — one file each, same namespaces.)

List handler:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.ListAllergyReactions;

public sealed class ListAllergyReactionsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListAllergyReactionsQuery, IReadOnlyList<AllergyReactionDto>>
{
    public async ValueTask<IReadOnlyList<AllergyReactionDto>> Handle(
        ListAllergyReactionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<Domain.AllergyReaction> q = dbContext.AllergyReactions
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (query.IsActive.HasValue)
        {
            q = q.Where(x => x.IsActive == query.IsActive.Value);
        }

        return await q
            .OrderBy(x => x.Term)
            .Select(x => new AllergyReactionDto(x.Id, x.Term, x.SnomedCode, x.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
```

Create/Update/Delete handlers follow the Drug handlers' shape (create → `AllergyReaction.Create(command.Term, command.SnomedCode)`; update → load-or-NotFound then `Update(command.Term, command.IsActive, command.SnomedCode)`; delete → soft `Delete`). Validators: `Term` NotEmpty MaxLength 256, `SnomedCode` MaxLength 32, `Id > 0` on update/delete. Endpoints on `/allergy-reactions` + `/allergy-reactions/{id:int}` with names `ListAllergyReactions`, `GetAllergyReactionById`, `CreateAllergyReaction`, `UpdateAllergyReaction`, `DeleteAllergyReaction`.

MedicationDoseUnits: identical slice set with `Name` (MaxLength 64) on `/medication-dose-units`, names `ListMedicationDoseUnits`, `GetMedicationDoseUnitById`, `CreateMedicationDoseUnit`, `UpdateMedicationDoseUnit`, `DeleteMedicationDoseUnit`. List handler is the AllergyReactions list handler with `MedicationDoseUnits`/`MedicationDoseUnitDto(x.Id, x.Name, x.IsActive)`/`OrderBy(x => x.Name)` substituted.

Permissions added to `AdministrationPermissions.cs` (+ `All` entries, View `IsBasic: true`):

```csharp
    public static class AllergyReactions
    {
        public const string Resource = "Administration.AllergyReactions";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class MedicationDoseUnits
    {
        public const string Resource = "Administration.MedicationDoseUnits";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }
```

`AdministrationModule.cs`: map all ten endpoints alongside the SmokingStatuses ones.

- [ ] **Step 4: Tests**

`src/Tests/Administration.Tests/Features/AllergyReactionHandlerTests.cs` and `MedicationDoseUnitHandlerTests.cs` — same roundtrip pattern as `DrugHandlerTests` (create → list → update → delete-hides). Write them BEFORE implementing Step 3's handlers, run to see the compile failure, then implement (red → green).

- [ ] **Step 5: Administration EF migration**

```bash
dotnet build src/FSH.Starter.slnx
dotnet ef migrations add AddDrugCatalogLookups \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context AdministrationDbContext \
  --output-dir Administration
dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply
```

Expected: creates `Drugs`, `AllergyReactions`, `MedicationDoseUnits` (+ seed INSERTs). Review the script for drops before applying (there must be none).

- [ ] **Step 6: Run all + commit**

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Administration.Tests --no-restore && dotnet test src/Tests/Architecture.Tests --no-restore
git add src/Modules/Administration src/Tests/Administration.Tests src/Host/FSH.Starter.Migrations.PostgreSQL
git commit -m "feat(administration): AllergyReaction + MedicationDoseUnit lookups with CRUD and migration"
```

---

### Task 9: Administration — RxNav search proxy + import (admin-controlled sync)

**Files:**
- Create: `src/Modules/Administration/Modules.Administration.Contracts/Dtos/RxNavDrugDto.cs`
- Create: `src/Modules/Administration/Modules.Administration.Contracts/v1/Drugs/SearchRxNavQuery.cs`
- Create: `src/Modules/Administration/Modules.Administration.Contracts/v1/Drugs/ImportDrugsCommand.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Drugs/SearchRxNav/{SearchRxNavQueryHandler,SearchRxNavQueryValidator,SearchRxNavEndpoint}.cs`
- Create: `src/Modules/Administration/Modules.Administration/Features/v1/Drugs/ImportDrugs/{ImportDrugsCommandHandler,ImportDrugsCommandValidator,ImportDrugsEndpoint}.cs`
- Modify: `src/Modules/Administration/Modules.Administration/AdministrationModule.cs` (HttpClient registration + endpoint mapping)
- Test: `src/Tests/Administration.Tests/Features/RxNavTests.cs`

**Interfaces:**
- Consumes: `AddHeroResilience(builder.Configuration)` IHttpClientBuilder extension (BuildingBlocks — do not modify), `Drug` entity (Task 7).
- Produces (used by Task 16):
  - `RxNavDrugDto(string RxCui, string Name, string? Tty)`
  - `SearchRxNavQuery(string Term) : IQuery<IReadOnlyList<RxNavDrugDto>>` → `GET api/v1/administration/drugs/rxnav?term=...` (permission `Drugs.Create` — admin-only sync, not the basic View)
  - `ImportDrugsCommand(IReadOnlyList<RxNavDrugDto> Items) : ICommand<int>` (returns count imported/updated) → `POST api/v1/administration/drugs/import` (permission `Drugs.Create`)
  - Named HttpClient `"RxNav"` with BaseAddress `https://rxnav.nlm.nih.gov/`

- [ ] **Step 1: Contracts**

`Contracts/Dtos/RxNavDrugDto.cs`:

```csharp
namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record RxNavDrugDto(string RxCui, string Name, string? Tty);
```

`Contracts/v1/Drugs/SearchRxNavQuery.cs`:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record SearchRxNavQuery(string Term) : IQuery<IReadOnlyList<RxNavDrugDto>>;
```

`Contracts/v1/Drugs/ImportDrugsCommand.cs`:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Drugs;

public sealed record ImportDrugsCommand(IReadOnlyList<RxNavDrugDto> Items) : ICommand<int>;
```

- [ ] **Step 2: HttpClient registration**

In `AdministrationModule.cs` `ConfigureServices` add (usings: `Microsoft.Extensions.DependencyInjection`, plus whatever namespace `AddHeroResilience` lives in — find it with `grep -rn "AddHeroResilience" src/BuildingBlocks/Web/HttpResilience`):

```csharp
        builder.Services.AddHttpClient("RxNav", client =>
        {
            client.BaseAddress = new Uri("https://rxnav.nlm.nih.gov/");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddHeroResilience(builder.Configuration);
```

- [ ] **Step 3: Failing test for the response mapper**

RxNav `GET /REST/drugs.json?name={term}` returns:

```json
{ "drugGroup": { "name": "lipitor", "conceptGroup": [
    { "tty": "SBD", "conceptProperties": [
        { "rxcui": "617314", "name": "atorvastatin 40 MG Oral Tablet [Lipitor]", "synonym": "Lipitor 40 MG Oral Tablet", "tty": "SBD", "language": "ENG", "suppress": "N", "umlscui": "" } ] } ] } }
```

Make the parsing a pure static method so it's unit-testable without HTTP: `SearchRxNavQueryHandler.ParseDrugsResponse(string json)`.

`src/Tests/Administration.Tests/Features/RxNavTests.cs`:

```csharp
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;
using Shouldly;
using Xunit;

namespace Administration.Tests.Features;

public sealed class RxNavTests
{
    [Fact]
    public void ParseDrugsResponse_Maps_ConceptProperties()
    {
        const string json = """
        {"drugGroup":{"name":"lipitor","conceptGroup":[
          {"tty":"SBD","conceptProperties":[
            {"rxcui":"617314","name":"atorvastatin 40 MG Oral Tablet [Lipitor]","synonym":"","tty":"SBD","language":"ENG","suppress":"N","umlscui":""}]},
          {"tty":"BPCK"}]}}
        """;

        IReadOnlyList<RxNavDrugDto> drugs = SearchRxNavQueryHandler.ParseDrugsResponse(json);

        drugs.Count.ShouldBe(1);
        drugs[0].RxCui.ShouldBe("617314");
        drugs[0].Name.ShouldBe("atorvastatin 40 MG Oral Tablet [Lipitor]");
        drugs[0].Tty.ShouldBe("SBD");
    }

    [Fact]
    public void ParseDrugsResponse_Empty_Group_Returns_Empty()
    {
        IReadOnlyList<RxNavDrugDto> drugs =
            SearchRxNavQueryHandler.ParseDrugsResponse("""{"drugGroup":{"name":"zzz"}}""");
        drugs.ShouldBeEmpty();
    }
}
```

Run `dotnet test src/Tests/Administration.Tests --no-restore` — expect compile failure.

- [ ] **Step 4: Implement**

`.../SearchRxNav/SearchRxNavQueryHandler.cs`:

```csharp
using System.Text.Json;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;

public sealed class SearchRxNavQueryHandler(IHttpClientFactory httpClientFactory)
    : IQueryHandler<SearchRxNavQuery, IReadOnlyList<RxNavDrugDto>>
{
    public async ValueTask<IReadOnlyList<RxNavDrugDto>> Handle(SearchRxNavQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        HttpClient client = httpClientFactory.CreateClient("RxNav");
        string json = await client
            .GetStringAsync(new Uri($"REST/drugs.json?name={Uri.EscapeDataString(query.Term)}", UriKind.Relative), cancellationToken)
            .ConfigureAwait(false);

        return ParseDrugsResponse(json);
    }

    /// <summary>Maps RxNav /REST/drugs.json to candidates. Internal shape: drugGroup.conceptGroup[].conceptProperties[].</summary>
    internal static IReadOnlyList<RxNavDrugDto> ParseDrugsResponse(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        var results = new List<RxNavDrugDto>();

        if (!doc.RootElement.TryGetProperty("drugGroup", out JsonElement drugGroup) ||
            !drugGroup.TryGetProperty("conceptGroup", out JsonElement conceptGroups))
        {
            return results;
        }

        foreach (JsonElement group in conceptGroups.EnumerateArray())
        {
            if (!group.TryGetProperty("conceptProperties", out JsonElement properties))
            {
                continue;
            }

            foreach (JsonElement concept in properties.EnumerateArray())
            {
                string? rxCui = concept.TryGetProperty("rxcui", out JsonElement c) ? c.GetString() : null;
                string? name = concept.TryGetProperty("name", out JsonElement n) ? n.GetString() : null;
                string? tty = concept.TryGetProperty("tty", out JsonElement t) ? t.GetString() : null;
                if (!string.IsNullOrWhiteSpace(rxCui) && !string.IsNullOrWhiteSpace(name))
                {
                    results.Add(new RxNavDrugDto(rxCui, name, tty));
                }
            }
        }

        return results;
    }
}
```

> `internal` + the test project needs visibility: check `Modules.Administration` for an existing `InternalsVisibleTo("Administration.Tests")`; if present the test can call it. If not, make `ParseDrugsResponse` `public static` — the test above assumes callable; adjust access accordingly.

`.../SearchRxNav/SearchRxNavQueryValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;

public sealed class SearchRxNavQueryValidator : AbstractValidator<SearchRxNavQuery>
{
    public SearchRxNavQueryValidator()
    {
        RuleFor(x => x.Term).NotEmpty().MinimumLength(3).MaximumLength(256);
    }
}
```

`.../SearchRxNav/SearchRxNavEndpoint.cs` — register BEFORE `/drugs/{id:int}` routes in the module so the literal `rxnav` segment wins:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;

public static class SearchRxNavEndpoint
{
    internal static RouteHandlerBuilder MapSearchRxNavEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/drugs/rxnav",
                async (string term, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new SearchRxNavQuery(term), ct)))
            .WithName("SearchRxNav")
            .WithSummary("Search the NIH RxNav API for drug concepts to import (admin-controlled sync)")
            .RequirePermission(AdministrationPermissions.Drugs.Create);
    }
}
```

`.../ImportDrugs/ImportDrugsCommandHandler.cs`:

```csharp
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Drugs.ImportDrugs;

public sealed class ImportDrugsCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<ImportDrugsCommand, int>
{
    public async ValueTask<int> Handle(ImportDrugsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var rxCuis = command.Items.Select(i => i.RxCui).ToList();
        Dictionary<string, Drug> existing = await dbContext.Drugs
            .Where(d => d.RxCui != null && rxCuis.Contains(d.RxCui))
            .ToDictionaryAsync(d => d.RxCui!, cancellationToken)
            .ConfigureAwait(false);

        int affected = 0;
        foreach (var item in command.Items)
        {
            if (existing.TryGetValue(item.RxCui, out Drug? drug))
            {
                drug.Update(item.Name, drug.RxAui, item.RxCui, item.Tty, "RXNORM", item.RxCui, isActive: true);
            }
            else
            {
                dbContext.Drugs.Add(Drug.Create(item.Name, rxAui: null, rxCui: item.RxCui, tty: item.Tty,
                    sab: "RXNORM", code: item.RxCui));
            }
            affected++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return affected;
    }
}
```

`.../ImportDrugs/ImportDrugsCommandValidator.cs`:

```csharp
using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Drugs;

namespace FSH.Modules.Administration.Features.v1.Drugs.ImportDrugs;

public sealed class ImportDrugsCommandValidator : AbstractValidator<ImportDrugsCommand>
{
    public ImportDrugsCommandValidator()
    {
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.RxCui).NotEmpty().MaximumLength(12);
            item.RuleFor(i => i.Name).NotEmpty().MaximumLength(2048);
            item.RuleFor(i => i.Tty).MaximumLength(20);
        });
    }
}
```

`.../ImportDrugs/ImportDrugsEndpoint.cs` — POST `/drugs/import` with body command, name `ImportDrugs`, permission `Drugs.Create`. Register before `/drugs/{id:int}` too.

`AdministrationModule.cs` — ordering:

```csharp
        group.MapSearchRxNavEndpoint();   // literal /drugs/rxnav before /drugs/{id:int}
        group.MapImportDrugsEndpoint();   // literal /drugs/import before /drugs/{id:int}
        // ...existing MapListDrugsEndpoint() etc. from Task 7 stay after these
```

Add an import-roundtrip test to `RxNavTests.cs` (same in-memory context helper as `DrugHandlerTests`):

```csharp
    [Fact]
    public async Task ImportDrugs_Upserts_By_RxCui()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new ImportDrugsCommandHandler(db);
        var item = new RxNavDrugDto("314076", "Lisinopril 10 MG Oral Tablet", "SCD");

        int first = await sut.Handle(new ImportDrugsCommand([item]), CancellationToken.None);
        int second = await sut.Handle(
            new ImportDrugsCommand([item with { Name = "Lisinopril 10 MG Oral Tablet (updated)" }]),
            CancellationToken.None);

        first.ShouldBe(1);
        second.ShouldBe(1);
        db.Drugs.Count(d => d.RxCui == "314076").ShouldBe(1);
        db.Drugs.Single(d => d.RxCui == "314076").Name.ShouldBe("Lisinopril 10 MG Oral Tablet (updated)");
    }
```

- [ ] **Step 5: Run + build green, commit**

```bash
dotnet build src/FSH.Starter.slnx && dotnet test src/Tests/Administration.Tests --no-restore && dotnet test src/Tests/Architecture.Tests --no-restore
git add src/Modules/Administration src/Tests/Administration.Tests
git commit -m "feat(administration): RxNav search proxy + admin-controlled drug import"
```

---

### Task 10: DbMigrator — `migrate-drug-catalog-from-mssql` verb

**Files:**
- Create: `src/Host/FSH.Starter.DbMigrator/MssqlMigration/MssqlDrugCatalogMapper.cs`
- Create: `src/Host/FSH.Starter.DbMigrator/MssqlMigration/MssqlDrugCatalogMigrationRunner.cs`
- Modify: `src/Host/FSH.Starter.DbMigrator/MigratorCommand.cs` (add verb to `ValidCommands` + help text)
- Modify: `src/Host/FSH.Starter.DbMigrator/Program.cs` (add a Step 4d block mirroring 4b)

**Interfaces:**
- Consumes: `AdministrationDbContext` (`Drugs`, `AllergyReactions`, `MedicationDoseUnits` from Tasks 7–8), tenant-resolution + Finbuckle-context pattern from `MssqlLookupMigrationRunner`.
- Produces: CLI verb `migrate-drug-catalog-from-mssql --source-connection <cs> --tenant <id> [--dry-run]`.

**Approach.** Mirror `MssqlLookupMigrationRunner`'s structure (tenant resolve → open `SqlConnection` → per-table try/catch → replace semantics → `setval` sequence reset), with one difference: RXNCONSO is millions of rows, so `Drugs` uses Npgsql **binary COPY** instead of row-by-row inserts, in a plain `NpgsqlConnection` opened from `dbContext.Database.GetConnectionString()`. The two small lookups use the existing parameterized-insert style.

**Source queries** (legacy Flex-era schema — RE-VERIFY names against the live BronstonChiro DB with `sp_help` before running; the RXNCONSO/Snomed tables may live in `Bronston` or `BronstonAuthenticatingDB`):

```sql
-- Drugs (huge; stream with SequentialAccess reader)
SELECT RXAUI, RXCUI, STR, TTY, SAB, CODE FROM dbo.RXNCONSO;

-- Allergy reactions (curated subset)
SELECT sa.saID, s.snoConceptID, s.snoTerm
FROM dbo.SnomedAssociations sa
JOIN dbo.Snomed s ON s.snoDescriptionID = sa.saSnomedDescriptionID
WHERE sa.saIsReaction = 1;

-- Dose units
SELECT mutID,
       COALESCE(NULLIF(mutCDISCSubmissionValue, ''), mutNCIPreferredTerm) AS UnitName
FROM dbo.MedicationUnitTypes;
```

- [ ] **Step 1: Mapper**

`MssqlDrugCatalogMapper.cs` — row records + readers:

```csharp
using Microsoft.Data.SqlClient;

namespace FSH.Starter.DbMigrator.MssqlMigration;

internal static class MssqlDrugCatalogMapper
{
    internal sealed record DrugRow(string? RxAui, string? RxCui, string Name, string? Tty, string? Sab, string? Code);
    internal sealed record ReactionRow(int Id, string? SnomedCode, string Term);
    internal sealed record DoseUnitRow(int Id, string Name);

    internal const string DrugsSql = "SELECT RXAUI, RXCUI, STR, TTY, SAB, CODE FROM dbo.RXNCONSO";

    internal const string ReactionsSql = """
        SELECT sa.saID, s.snoConceptID, s.snoTerm
        FROM dbo.SnomedAssociations sa
        JOIN dbo.Snomed s ON s.snoDescriptionID = sa.saSnomedDescriptionID
        WHERE sa.saIsReaction = 1
        """;

    internal const string DoseUnitsSql = """
        SELECT mutID, COALESCE(NULLIF(mutCDISCSubmissionValue, ''), mutNCIPreferredTerm) AS UnitName
        FROM dbo.MedicationUnitTypes
        """;

    internal static DrugRow ReadDrug(SqlDataReader rdr) => new(
        RxAui: rdr.IsDBNull(0) ? null : Convert.ToString(rdr.GetValue(0), System.Globalization.CultureInfo.InvariantCulture),
        RxCui: rdr.IsDBNull(1) ? null : Convert.ToString(rdr.GetValue(1), System.Globalization.CultureInfo.InvariantCulture),
        Name: rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2),
        Tty: rdr.IsDBNull(3) ? null : rdr.GetString(3),
        Sab: rdr.IsDBNull(4) ? null : rdr.GetString(4),
        Code: rdr.IsDBNull(5) ? null : Convert.ToString(rdr.GetValue(5), System.Globalization.CultureInfo.InvariantCulture));

    internal static ReactionRow ReadReaction(SqlDataReader rdr) => new(
        Id: Convert.ToInt32(rdr.GetValue(0), System.Globalization.CultureInfo.InvariantCulture),
        SnomedCode: rdr.IsDBNull(1) ? null : Convert.ToString(rdr.GetValue(1), System.Globalization.CultureInfo.InvariantCulture),
        Term: rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2));

    internal static DoseUnitRow ReadDoseUnit(SqlDataReader rdr) => new(
        Id: Convert.ToInt32(rdr.GetValue(0), System.Globalization.CultureInfo.InvariantCulture),
        Name: rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1));
}
```

(RXAUI/RXCUI/snoConceptID may be numeric columns in MSSQL — `Convert.ToString(GetValue(...))` handles both numeric and varchar without caring.)

- [ ] **Step 2: Runner**

`MssqlDrugCatalogMigrationRunner.cs` — key logic (copy the class scaffolding — tenant resolve, scope creation, Finbuckle context set, error-file writing — from `MssqlLookupMigrationRunner` verbatim, then these table steps):

```csharp
// 1. AllergyReactions + MedicationDoseUnits — replace semantics via dbContext.Database
//    .ExecuteSqlRawAsync, exactly like MssqlLookupMigrationRunner does per table:
//    DELETE FROM administration."AllergyReactions";
//    INSERT ... (Id, Term, SnomedCode, IsActive, IsDeleted) VALUES (@p0..) per row (preserve saID as Id);
//    SELECT setval(pg_get_serial_sequence('administration."AllergyReactions"', 'Id'), maxId);
//    Skip empty Term rows. Truncate Term to 256 chars.
//    Same for MedicationDoseUnits with mutID/UnitName (truncate 64).

// 2. Drugs — stream + COPY:
string? connString = dbContext.Database.GetConnectionString();
await using var pg = new NpgsqlConnection(connString);
await pg.OpenAsync(ct).ConfigureAwait(false);

if (!dryRun)
{
    await using (var del = new NpgsqlCommand("DELETE FROM administration.\"Drugs\"", pg))
    {
        await del.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}

long count = 0;
await using (var cmd = new SqlCommand(MssqlDrugCatalogMapper.DrugsSql, conn))
await using (SqlDataReader rdr = await cmd.ExecuteReaderAsync(
    System.Data.CommandBehavior.SequentialAccess, ct).ConfigureAwait(false))
{
    if (dryRun)
    {
        while (await rdr.ReadAsync(ct).ConfigureAwait(false)) count++;
    }
    else
    {
        // Column list must match the EF table exactly — check the generated migration for
        // column names/order; Id is identity so it is OMITTED from COPY and auto-assigned.
        await using var writer = await pg.BeginBinaryImportAsync(
            "COPY administration.\"Drugs\" (\"Name\", \"RxAui\", \"RxCui\", \"Tty\", \"Sab\", \"Code\", " +
            "\"IsActive\", \"CreatedAtUtc\", \"IsDeleted\") FROM STDIN (FORMAT BINARY)", ct)
            .ConfigureAwait(false);

        while (await rdr.ReadAsync(ct).ConfigureAwait(false))
        {
            var row = MssqlDrugCatalogMapper.ReadDrug(rdr);
            if (string.IsNullOrWhiteSpace(row.Name)) continue;

            await writer.StartRowAsync(ct).ConfigureAwait(false);
            await writer.WriteAsync(Truncate(row.Name, 2048), NpgsqlDbType.Varchar, ct).ConfigureAwait(false);
            await WriteNullable(writer, Truncate(row.RxAui, 12), ct).ConfigureAwait(false);
            await WriteNullable(writer, Truncate(row.RxCui, 12), ct).ConfigureAwait(false);
            await WriteNullable(writer, Truncate(row.Tty, 20), ct).ConfigureAwait(false);
            await WriteNullable(writer, Truncate(row.Sab, 40), ct).ConfigureAwait(false);
            await WriteNullable(writer, Truncate(row.Code, 64), ct).ConfigureAwait(false);
            await writer.WriteAsync(true, NpgsqlDbType.Boolean, ct).ConfigureAwait(false);
            await writer.WriteAsync(DateTime.UtcNow, NpgsqlDbType.TimestampTz, ct).ConfigureAwait(false);
            await writer.WriteAsync(false, NpgsqlDbType.Boolean, ct).ConfigureAwait(false);
            count++;
        }

        await writer.CompleteAsync(ct).ConfigureAwait(false);
    }
}
```

with helpers:

```csharp
private static string? Truncate(string? value, int max) =>
    value is null ? null : (value.Length <= max ? value : value[..max]);

private static async Task WriteNullable(NpgsqlBinaryImporter writer, string? value, CancellationToken ct)
{
    if (value is null) await writer.WriteNullAsync(ct).ConfigureAwait(false);
    else await writer.WriteAsync(value, NpgsqlDbType.Varchar, ct).ConfigureAwait(false);
}
```

Important details:
- Duplicate RXAUI guard: the unique filtered index on `RxAui` will fail COPY on dupes. Track a `HashSet<string> seenRxAuis` and skip rows whose non-null RxAui was already written (log the skip count).
- `CreatedAtUtc` column type: check the generated migration — if it's `timestamp with time zone` use `TimestampTz` as shown; if `timestamp without time zone` use `Timestamp`.
- Progress log every 100k rows via `Console.Out.WriteLineAsync`.
- Per-table try/catch with the same error-record file the lookup runner writes.

- [ ] **Step 3: Wire the verb**

`MigratorCommand.cs`: add `"migrate-drug-catalog-from-mssql"` to `ValidCommands` and a help line:

```text
  migrate-drug-catalog-from-mssql
                      Copy RXNCONSO → Drugs (COPY), SnomedAssociations → AllergyReactions,
                      MedicationUnitTypes → MedicationDoseUnits. Run before migrate-from-mssql.
                      Requires --source-connection and --tenant. Supports --dry-run.
```

`Program.cs`: add a Step 4d block after 4c mirroring the 4b (`migrate-lookups-from-mssql`) block exactly — same required-arg validation messages with a `[mssql-drug-catalog]` prefix, constructing `MssqlDrugCatalogMigrationRunner` and returning its exit code.

- [ ] **Step 4: Build + dry-run + verify**

```bash
dotnet build src/FSH.Starter.slnx
dotnet run --project src/Host/FSH.Starter.DbMigrator -- migrate-drug-catalog-from-mssql --dry-run \
  --source-connection "Server=(localdb)\mssqllocaldb;Database=Bronston;Integrated Security=true;TrustServerCertificate=true" \
  --tenant root
```

Expected (with the local BronstonChiro DB up): per-table row counts, zero writes. If a table errors (e.g. RXNCONSO lives in the other database), adjust the SQL/database per the RE-VERIFY note and re-run. If no source DB is available locally, dry-run wiring is still verified by the arg-validation errors; note it in the commit message.

- [ ] **Step 5: Commit**

```bash
git add src/Host/FSH.Starter.DbMigrator
git commit -m "feat(migrator): migrate-drug-catalog-from-mssql verb (RXNCONSO, reactions, dose units)"
```

---

### Task 11: DbMigrator — extend `migrate-from-mssql` with clinical lists

**Files:**
- Create: `src/Host/FSH.Starter.DbMigrator/MssqlMigration/MssqlClinicalListMapper.cs`
- Modify: `src/Host/FSH.Starter.DbMigrator/MssqlMigration/MssqlPatientMigrationRunner.cs` (add a clinical-lists phase after patients are upserted)

**Interfaces:**
- Consumes: `PatientDbContext` DbSets from Tasks 1–4; `Patient.LegacyUniqueId` (existing) to resolve `pUniqueID` → `Patient.Id`.
- Produces: `migrate-from-mssql` now also copies PatientAllergies, PatientMedications, PatientNotes, MedicationReconciledDates.

**Approach & source queries** (RE-VERIFY column names against the live DB / `GenerateDatabase` scripts before implementing — especially `PatientMedications`, whose exact drug-name storage may involve a `MedicationList` (`ml*`) table; the legacy `PatientMedications_Set` sproc takes `@mlMedicationName`. Read the body of `PatientMedications_GetByPatientID` in the source DB to get the SELECT column list, then adjust `MedsSql`):

```sql
SELECT paID, paPatientUniqueID, paDrugName, paRXAUI, paReaction, paComments,
       paActive, paDateNoted, paCreatedDate, paModifiedDate
FROM dbo.PatientAllergies;

-- Draft; RE-VERIFY against PatientMedications_GetByPatientID output
SELECT pmID, pmPatientUniqueID, pmDrugName, pmRXAUI, pmNDC, pmPrescriber,
       pmStartDate, pmEndDate, pmDoseValue, pmDoseUnitID, pmDosePeriodValue,
       pmDosePeriodUnit, pmInstructions, pmIndication, pmActive,
       pmCreatedDate, pmModifiedDate
FROM dbo.PatientMedications;

SELECT pnID, pnPatientUniqueID, pnName, pnDescription, pnCreatedBy,
       pnCreatedDate, pnDeleted, pnMedicalAlert
FROM dbo.PatientNotes;

SELECT mrdID, mrdPatientUniqueID, mrdDate FROM dbo.MedicationReconciledDates;
```

These tables are expected plaintext (not in the known symmetric-key encrypted-column list from the patient migration); confirm on first dry-run — if any column comes back `varbinary`, wrap it in the same `DecryptByKey` pattern `MssqlPatientMapper` uses.

- [ ] **Step 1: Implement the mapper + runner phase**

`MssqlClinicalListMapper.cs`: for each of the four tables, a `record` row type + `Read(SqlDataReader)` + the SQL constant (same style as `MssqlDrugCatalogMapper`). Dates: `rdr.IsDBNull(n) ? (DateTime?)null : rdr.GetDateTime(n)`; bools may be `bit` or `int` — use `Convert.ToBoolean(rdr.GetValue(n), CultureInfo.InvariantCulture)`.

In `MssqlPatientMigrationRunner`, after the patient upsert loop completes (find its "done" log), add a `MigrateClinicalListsAsync(conn, dbContext, dryRun, ct)` phase that:

1. Builds the id map once:

```csharp
Dictionary<long, Guid> patientByLegacyId = await dbContext.Patients
    .Where(p => p.LegacyUniqueId != null)
    .ToDictionaryAsync(p => p.LegacyUniqueId!.Value, p => p.Id, ct)
    .ConfigureAwait(false);
```

2. For each table: read all source rows; skip rows whose `p*PatientUniqueID` is not in the map (count + log skips); **idempotency** — delete previously-migrated rows for mapped patients before insert (`DELETE FROM patient."PatientAllergies" WHERE "PatientId" = ANY(@ids)` batched), then insert via the domain factories:

```csharp
foreach (var row in allergyRows)
{
    if (!patientByLegacyId.TryGetValue(row.PatientUniqueId, out Guid patientId)) { skipped++; continue; }
    dbContext.PatientAllergies.Add(Domain.PatientAllergy.Create(
        patientId, row.DrugName ?? "(unknown)", row.RxAui, row.Reaction, row.Comments,
        row.DateNoted ?? row.CreatedDate ?? DateTime.UtcNow, row.Active,
        createdByUserId: null, createdByName: "BackChart migration"));
}
await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
```

(same for medications — `DoseValue`/`DosePeriodValue` parsed with `decimal.TryParse` invariant; notes — use `PatientNote.Create` then call `note.Delete("BackChart migration")` when `pnDeleted` is true; reconciled dates — `MedicationReconciledDate.Create(patientId, row.Date, null, "BackChart migration")`). Batch `SaveChangesAsync` every 500 adds to keep the change tracker small (`dbContext.ChangeTracker.Clear()` after each save).

3. Patient flags `pNoAllergies`/`pNoMedications`: check `MssqlPatientMapper` — it already projects `HasNoKnownAllergies`/`HasNoKnownMedications` into `Patient.Create` (the columns exist since Sprint 1). If it does, nothing to do; if the projection is missing, add the two columns to its SELECT + mapping.

4. Dry-run: count rows + resolvable patients per table, write nothing.

- [ ] **Step 2: Build + dry-run**

```bash
dotnet build src/FSH.Starter.slnx
dotnet run --project src/Host/FSH.Starter.DbMigrator -- migrate-from-mssql --dry-run \
  --source-connection "Server=(localdb)\mssqllocaldb;Database=Bronston;Integrated Security=true;TrustServerCertificate=true" \
  --tenant root
```

Expected: existing patient dry-run output plus four new per-table lines (rows read / resolvable / skipped).

- [ ] **Step 3: Commit**

```bash
git add src/Host/FSH.Starter.DbMigrator
git commit -m "feat(migrator): migrate patient allergies, medications, notes, reconciled dates"
```

---

### Task 12: Frontend — API modules + permission mirrors

**Files:**
- Create: `clients/dashboard/src/api/allergies.ts`
- Create: `clients/dashboard/src/api/medications.ts`
- Create: `clients/dashboard/src/api/patient-notes.ts`
- Modify: `clients/dashboard/src/lib/patient-permissions.ts`

**Interfaces:**
- Consumes: routes from Tasks 1–5; `apiFetch`, `PagedResponse` (existing).
- Produces (used by Tasks 13–15): the exported types/functions below, and `ALLERGY_PERMISSIONS` / `MEDICATION_PERMISSIONS` / `NOTE_PERMISSIONS`.

- [ ] **Step 1: `clients/dashboard/src/api/allergies.ts`**

```ts
import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type PatientAllergy = {
  id: string;
  patientId: string;
  drugName: string;
  rxAui?: string | null;
  reaction?: string | null;
  comments?: string | null;
  dateNoted: string;
  isActive: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

export type SearchAllergiesParams = {
  patientId: string;
  includeInactive?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientAllergies(
  params: SearchAllergiesParams,
): Promise<PagedResponse<PatientAllergy>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.includeInactive) query.set("includeInactive", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientAllergy>>(`/api/v1/patient/allergies?${query.toString()}`);
}

export type CreateAllergyInput = {
  patientId: string;
  drugName: string;
  rxAui?: string | null;
  reaction?: string | null;
  comments?: string | null;
  dateNoted: string;
  isActive: boolean;
};

export async function createAllergy(input: CreateAllergyInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/allergies", {
    method: "POST",
    body: JSON.stringify({
      patientId: input.patientId,
      drugName: input.drugName,
      rxAui: input.rxAui ?? null,
      reaction: input.reaction ?? null,
      comments: input.comments ?? null,
      dateNoted: input.dateNoted,
      isActive: input.isActive,
    }),
  });
}

export type UpdateAllergyInput = CreateAllergyInput & { allergyId: string };

export async function updateAllergy(input: UpdateAllergyInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/allergies/${encodeURIComponent(input.allergyId)}`, {
    method: "PUT",
    body: JSON.stringify({
      allergyId: input.allergyId,
      drugName: input.drugName,
      rxAui: input.rxAui ?? null,
      reaction: input.reaction ?? null,
      comments: input.comments ?? null,
      dateNoted: input.dateNoted,
      isActive: input.isActive,
    }),
  });
}

export async function setNoKnownAllergies(patientId: string, value: boolean): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/patients/${encodeURIComponent(patientId)}/no-known-allergies`,
    { method: "PUT", body: JSON.stringify({ value }) },
  );
}
```

(Note: `UpdateAllergyInput` includes `patientId` via the intersection — harmless extra body field is NOT sent because the body above lists fields explicitly.)

- [ ] **Step 2: `clients/dashboard/src/api/medications.ts`**

```ts
import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type PatientMedication = {
  id: string;
  patientId: string;
  drugName: string;
  rxAui?: string | null;
  rxCode?: string | null;
  ndc?: string | null;
  prescriber?: string | null;
  startDate: string;
  endDate?: string | null;
  doseValue?: number | null;
  doseUnitId?: number | null;
  dosePeriodValue?: number | null;
  dosePeriodUnit?: string | null;
  instructions?: string | null;
  indication?: string | null;
  isActive: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

export type SearchMedicationsParams = {
  patientId: string;
  includeInactive?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientMedications(
  params: SearchMedicationsParams,
): Promise<PagedResponse<PatientMedication>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.includeInactive) query.set("includeInactive", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientMedication>>(
    `/api/v1/patient/medications?${query.toString()}`,
  );
}

export type MedicationFields = {
  drugName: string;
  rxAui?: string | null;
  rxCode?: string | null;
  ndc?: string | null;
  prescriber?: string | null;
  startDate: string;
  endDate?: string | null;
  doseValue?: number | null;
  doseUnitId?: number | null;
  dosePeriodValue?: number | null;
  dosePeriodUnit?: string | null;
  instructions?: string | null;
  indication?: string | null;
  isActive: boolean;
};

function medicationBody(fields: MedicationFields): Record<string, unknown> {
  return {
    drugName: fields.drugName,
    rxAui: fields.rxAui ?? null,
    rxCode: fields.rxCode ?? null,
    ndc: fields.ndc ?? null,
    prescriber: fields.prescriber ?? null,
    startDate: fields.startDate,
    endDate: fields.endDate ?? null,
    doseValue: fields.doseValue ?? null,
    doseUnitId: fields.doseUnitId ?? null,
    dosePeriodValue: fields.dosePeriodValue ?? null,
    dosePeriodUnit: fields.dosePeriodUnit ?? null,
    instructions: fields.instructions ?? null,
    indication: fields.indication ?? null,
    isActive: fields.isActive,
  };
}

export type CreateMedicationInput = MedicationFields & { patientId: string };

export async function createMedication(input: CreateMedicationInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/medications", {
    method: "POST",
    body: JSON.stringify({ patientId: input.patientId, ...medicationBody(input) }),
  });
}

export type UpdateMedicationInput = MedicationFields & { medicationId: string };

export async function updateMedication(input: UpdateMedicationInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/medications/${encodeURIComponent(input.medicationId)}`, {
    method: "PUT",
    body: JSON.stringify({ medicationId: input.medicationId, ...medicationBody(input) }),
  });
}

export async function setNoKnownMedications(patientId: string, value: boolean): Promise<void> {
  await apiFetch<void>(
    `/api/v1/patient/patients/${encodeURIComponent(patientId)}/no-known-medications`,
    { method: "PUT", body: JSON.stringify({ value }) },
  );
}

export type MedicationReconciledDate = {
  id: string;
  patientId: string;
  reconciledOn: string;
  createdByName?: string | null;
  createdAtUtc: string;
};

export function getMedicationReconciledDates(
  patientId: string,
): Promise<MedicationReconciledDate[]> {
  const query = new URLSearchParams({ patientId });
  return apiFetch<MedicationReconciledDate[]>(
    `/api/v1/patient/medication-reconciliations?${query.toString()}`,
  );
}

export async function markMedicationsReconciled(patientId: string): Promise<string> {
  return apiFetch<string>("/api/v1/patient/medication-reconciliations", {
    method: "POST",
    body: JSON.stringify({ patientId }),
  });
}

/** Legacy "Info" button — MedlinePlus Connect lookup by RxNorm code. */
export function medlinePlusUrl(rxCode: string): string {
  return (
    "https://connect.medlineplus.gov/application?mainSearchCriteria.v.cs=2.16.840.1.113883.6.88" +
    `&mainSearchCriteria.v.c=${encodeURIComponent(rxCode)}&informationRecipient.languageCode.c=en`
  );
}
```

- [ ] **Step 3: `clients/dashboard/src/api/patient-notes.ts`**

```ts
import { apiFetch } from "@/lib/api-client";
import type { PagedResponse } from "@/api/catalog";

export type PatientNote = {
  id: string;
  patientId: string;
  name: string;
  description?: string | null;
  isMedicalAlert: boolean;
  createdByName?: string | null;
  createdAtUtc: string;
  updatedByName?: string | null;
  updatedAtUtc?: string | null;
};

export type SearchNotesParams = {
  patientId: string;
  medicalAlertsOnly?: boolean;
  includeDeleted?: boolean;
  pageNumber?: number;
  pageSize?: number;
};

export function searchPatientNotes(params: SearchNotesParams): Promise<PagedResponse<PatientNote>> {
  const query = new URLSearchParams();
  query.set("patientId", params.patientId);
  if (params.medicalAlertsOnly) query.set("medicalAlertsOnly", "true");
  if (params.includeDeleted) query.set("includeDeleted", "true");
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 100));
  return apiFetch<PagedResponse<PatientNote>>(`/api/v1/patient/notes?${query.toString()}`);
}

export type CreateNoteInput = {
  patientId: string;
  name: string;
  description?: string | null;
  isMedicalAlert: boolean;
};

export async function createNote(input: CreateNoteInput): Promise<string> {
  return apiFetch<string>("/api/v1/patient/notes", {
    method: "POST",
    body: JSON.stringify({
      patientId: input.patientId,
      name: input.name,
      description: input.description ?? null,
      isMedicalAlert: input.isMedicalAlert,
    }),
  });
}

export type UpdateNoteInput = {
  noteId: string;
  name: string;
  description?: string | null;
  isMedicalAlert: boolean;
};

export async function updateNote(input: UpdateNoteInput): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/notes/${encodeURIComponent(input.noteId)}`, {
    method: "PUT",
    body: JSON.stringify({
      noteId: input.noteId,
      name: input.name,
      description: input.description ?? null,
      isMedicalAlert: input.isMedicalAlert,
    }),
  });
}

export async function deleteNote(id: string): Promise<void> {
  await apiFetch<void>(`/api/v1/patient/notes/${encodeURIComponent(id)}`, { method: "DELETE" });
}
```

- [ ] **Step 4: Permission mirrors**

Append to `clients/dashboard/src/lib/patient-permissions.ts`:

```ts
export const ALLERGY_PERMISSIONS = {
  view:   "Permissions.Patient.Allergies.View",
  create: "Permissions.Patient.Allergies.Create",
  update: "Permissions.Patient.Allergies.Update",
  delete: "Permissions.Patient.Allergies.Delete",
} as const;

export type AllergyPermissionKey = keyof typeof ALLERGY_PERMISSIONS;

export const MEDICATION_PERMISSIONS = {
  view:   "Permissions.Patient.Medications.View",
  create: "Permissions.Patient.Medications.Create",
  update: "Permissions.Patient.Medications.Update",
  delete: "Permissions.Patient.Medications.Delete",
} as const;

export type MedicationPermissionKey = keyof typeof MEDICATION_PERMISSIONS;

export const NOTE_PERMISSIONS = {
  view:   "Permissions.Patient.Notes.View",
  create: "Permissions.Patient.Notes.Create",
  update: "Permissions.Patient.Notes.Update",
  delete: "Permissions.Patient.Notes.Delete",
} as const;

export type NotePermissionKey = keyof typeof NOTE_PERMISSIONS;
```

- [ ] **Step 5: Typecheck + commit**

```bash
cd clients/dashboard && npx tsc -b --noEmit && npx eslint src/api/allergies.ts src/api/medications.ts src/api/patient-notes.ts src/lib/patient-permissions.ts
git add clients/dashboard/src/api clients/dashboard/src/lib/patient-permissions.ts
git commit -m "feat(dashboard): API modules + permission mirrors for allergies, medications, notes"
```

---

### Task 13: Frontend — drug picker + Allergy List dialog + chart button

**Files:**
- Create: `clients/dashboard/src/pages/patient-charts/drug-picker.tsx`
- Create: `clients/dashboard/src/pages/patient-charts/allergy-dialog.tsx`
- Create: `clients/dashboard/src/pages/patient-charts/allergy-list-dialog.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx` (Allergy List button + dialog mount)
- Modify: `clients/dashboard/src/api/administration.ts` (add `listDrugs` + `listAllergyReactions` + `listMedicationDoseUnits` + types — needed by pickers; full admin CRUD functions land in Task 16)

**Interfaces:**
- Consumes: `searchPatientAllergies`/`createAllergy`/`updateAllergy`/`setNoKnownAllergies` (Task 12), `getPatientById` (existing — its returned type already exposes `hasNoKnownAllergies` and `hasNoKnownMedications` booleans, confirmed in `clients/dashboard/src/api/patients.ts:155-156`).
- Produces: `<DrugPicker value onChange />` shared component (used by Task 14), `<AllergyListDialog patientId open onClose />`.

- [ ] **Step 1: administration.ts picker functions**

Append to `clients/dashboard/src/api/administration.ts` (follow the file's existing type/function style):

```ts
export type DrugDto = {
  id: number;
  name: string;
  rxAui?: string | null;
  rxCui?: string | null;
  tty?: string | null;
  sab?: string | null;
  code?: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc?: string | null;
};

export type ListDrugsParams = {
  search?: string;
  isActive?: boolean | null;
  pageNumber?: number;
  pageSize?: number;
};

export function listDrugs(params: ListDrugsParams = {}): Promise<PagedResponse<DrugDto>> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined && params.isActive !== null)
    query.set("isActive", String(params.isActive));
  query.set("pageNumber", String(params.pageNumber ?? 1));
  query.set("pageSize", String(params.pageSize ?? 20));
  return apiFetch<PagedResponse<DrugDto>>(`/api/v1/administration/drugs?${query.toString()}`);
}

export type AllergyReactionDto = {
  id: number;
  term: string;
  snomedCode?: string | null;
  isActive: boolean;
};

export function listAllergyReactions(params: { isActive?: boolean } = {}): Promise<AllergyReactionDto[]> {
  const query = new URLSearchParams();
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  return apiFetch<AllergyReactionDto[]>(
    `/api/v1/administration/allergy-reactions?${query.toString()}`,
  );
}

export type MedicationDoseUnitDto = {
  id: number;
  name: string;
  isActive: boolean;
};

export function listMedicationDoseUnits(
  params: { isActive?: boolean } = {},
): Promise<MedicationDoseUnitDto[]> {
  const query = new URLSearchParams();
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  return apiFetch<MedicationDoseUnitDto[]>(
    `/api/v1/administration/medication-dose-units?${query.toString()}`,
  );
}
```

- [ ] **Step 2: DrugPicker component**

`clients/dashboard/src/pages/patient-charts/drug-picker.tsx` (mirrors the ProblemDialog DX-picker interaction; free text allowed like legacy):

```tsx
import { useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { listDrugs } from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export type DrugSelection = {
  name: string;
  rxAui: string | null;
  rxCui: string | null;
};

type Props = {
  value: DrugSelection | null;
  onChange(value: DrugSelection | null): void;
  label?: string;
};

/** Drug-catalog search picker used by the allergy and medication dialogs.
 *  Free text is allowed (legacy allowed unlisted drug names). */
export function DrugPicker({ value, onChange, label = "Drug" }: Props) {
  const [search, setSearch] = useState("");

  const drugsQuery = useQuery({
    queryKey: ["drug-search", search],
    queryFn: () => listDrugs({ search, isActive: true, pageSize: 50 }),
    enabled: search.length >= 2,
  });

  const options = useMemo(() => drugsQuery.data?.items ?? [], [drugsQuery.data]);

  return (
    <div className="space-y-2">
      <label className="text-[11.5px] font-semibold uppercase tracking-wider text-[var(--color-muted-foreground)]">
        {label}
      </label>
      {value ? (
        <div className="flex items-start justify-between gap-2 rounded-lg border border-[var(--color-border)] p-2.5">
          <div className="min-w-0">
            <p className="text-[13px] font-medium">{value.name}</p>
            {value.rxCui && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">RxCUI {value.rxCui}</p>
            )}
          </div>
          <Button type="button" variant="ghost" size="xs" onClick={() => onChange(null)}>
            Change
          </Button>
        </div>
      ) : (
        <>
          <Input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search the drug catalog (min 2 chars)…"
            autoFocus
          />
          {search.length >= 2 && (
            <ul className="max-h-40 overflow-y-auto rounded-md border border-[var(--color-border)] bg-[var(--color-card)]">
              {options.map((d) => (
                <li key={d.id}>
                  <button
                    type="button"
                    onClick={() => {
                      onChange({ name: d.name, rxAui: d.rxAui ?? null, rxCui: d.rxCui ?? null });
                      setSearch("");
                    }}
                    className="w-full px-3 py-1.5 text-left text-[12px] hover:bg-[var(--color-accent)]"
                  >
                    {d.name}
                    {d.tty ? ` (${d.tty})` : ""}
                  </button>
                </li>
              ))}
              <li>
                <button
                  type="button"
                  onClick={() => {
                    onChange({ name: search.trim(), rxAui: null, rxCui: null });
                    setSearch("");
                  }}
                  className="w-full px-3 py-1.5 text-left text-[12px] italic text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)]"
                >
                  Use “{search.trim()}” as typed
                </button>
              </li>
            </ul>
          )}
        </>
      )}
    </div>
  );
}
```

- [ ] **Step 3: AllergyDialog (add/edit)**

`clients/dashboard/src/pages/patient-charts/allergy-dialog.tsx`:

```tsx
import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { createAllergy, updateAllergy, type PatientAllergy } from "@/api/allergies";
import { listAllergyReactions } from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field, type ComboboxOption } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { DrugPicker, type DrugSelection } from "@/pages/patient-charts/drug-picker";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  /** Edit mode when set. */
  allergy?: PatientAllergy | null;
  /** Whether the current user may flip Active/Inactive (Allergies.Delete, legacy ALLERGYLISTDELETE). */
  canToggleActive: boolean;
};

export function AllergyDialog({ patientId, open, onClose, allergy, canToggleActive }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!allergy;

  const [drug, setDrug] = useState<DrugSelection | null>(null);
  const [reaction, setReaction] = useState("");
  const [comments, setComments] = useState("");
  const [dateNoted, setDateNoted] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (!open) return;
    if (allergy) {
      setDrug({ name: allergy.drugName, rxAui: allergy.rxAui ?? null, rxCui: null });
      setReaction(allergy.reaction ?? "");
      setComments(allergy.comments ?? "");
      setDateNoted(allergy.dateNoted ? allergy.dateNoted.slice(0, 10) : "");
      setIsActive(allergy.isActive);
    } else {
      setDrug(null);
      setReaction("");
      setComments("");
      setDateNoted(new Date().toISOString().slice(0, 10));
      setIsActive(true);
    }
  }, [open, allergy]);

  const reactionsQuery = useQuery({
    queryKey: ["allergy-reactions", "active"],
    queryFn: () => listAllergyReactions({ isActive: true }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const reactionOptions: ComboboxOption[] = (reactionsQuery.data ?? []).map((r) => ({
    value: String(r.id),
    label: r.term,
  }));

  // Legacy behavior: the SNOMED picker appends terms into the single Reaction text field.
  const appendReaction = (id: string | null) => {
    if (!id) return;
    const term = (reactionsQuery.data ?? []).find((r) => String(r.id) === id)?.term;
    if (!term) return;
    setReaction((prev) => {
      const parts = prev.split(",").map((p) => p.trim()).filter(Boolean);
      if (parts.includes(term)) return prev;
      return [...parts, term].join(", ");
    });
  };

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-allergies", patientId] });
    void queryClient.invalidateQueries({ queryKey: ["patient", patientId] });
  };

  const createMutation = useMutation({
    mutationFn: createAllergy,
    onSuccess: () => {
      toast.success("Allergy added.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to add allergy.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateAllergy,
    onSuccess: () => {
      toast.success("Allergy updated.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to update allergy.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!drug?.name || !dateNoted) return;
    const fields = {
      patientId,
      drugName: drug.name,
      rxAui: drug.rxAui,
      reaction: reaction.trim() || null,
      comments: comments.trim() || null,
      dateNoted,
      isActive,
    };
    if (isEdit && allergy) {
      updateMutation.mutate({ ...fields, allergyId: allergy.id });
    } else {
      createMutation.mutate(fields);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit Allergy" : "Add Allergy"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <DrugPicker value={drug} onChange={setDrug} label="Drug Name" />

            <Field id="allergy-reaction-pick" label="Add Reaction (SNOMED list)">
              <Combobox
                id="allergy-reaction-pick"
                label="Add Reaction"
                value={null}
                onChange={appendReaction}
                options={reactionOptions}
                placeholder="Pick to append…"
              />
            </Field>

            <Field id="allergy-reaction" label="Reaction">
              <Input
                id="allergy-reaction"
                type="text"
                value={reaction}
                maxLength={1000}
                onChange={(e) => setReaction(e.target.value)}
                placeholder="e.g. Rash, Hives"
              />
            </Field>

            <Field id="allergy-date-noted" label="Date Noted">
              <Input
                id="allergy-date-noted"
                type="date"
                value={dateNoted}
                onChange={(e) => setDateNoted(e.target.value)}
                required
              />
            </Field>

            <Field id="allergy-comments" label="Comments">
              <Textarea
                id="allergy-comments"
                value={comments}
                onChange={(e) => setComments(e.target.value)}
                rows={3}
                maxLength={4000}
              />
            </Field>

            {canToggleActive && (
              <div className="flex items-center gap-4 text-[13px]">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="allergy-active"
                    checked={isActive}
                    onChange={() => setIsActive(true)}
                  />
                  <span>Active</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="allergy-active"
                    checked={!isActive}
                    onChange={() => setIsActive(false)}
                  />
                  <span>Inactive</span>
                </label>
              </div>
            )}

            {isEdit && allergy?.createdByName && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                Created: {allergy.createdByName}
                {allergy.updatedByName ? ` · Last modified: ${allergy.updatedByName}` : ""}
              </p>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !drug?.name || !dateNoted}>
              {isPending ? "Saving…" : isEdit ? "Save Changes" : "Add Allergy"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

> `Combobox` with `value={null}`: check the component's prop type in `@/components/list` — if it requires a string, pass `""` and reset after `onChange`. Mirror an existing "action-style" combobox usage if one exists; otherwise controlled-reset is fine.

- [ ] **Step 4: AllergyListDialog**

`clients/dashboard/src/pages/patient-charts/allergy-list-dialog.tsx` (mirror `ProblemListDialog`; adds the Set-No-Allergies checkbox):

```tsx
import { useMemo, useState } from "react";
import { keepPreviousData, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Pencil, Plus } from "lucide-react";
import { toast } from "sonner";
import { searchPatientAllergies, setNoKnownAllergies, type PatientAllergy } from "@/api/allergies";
import { getPatientById } from "@/api/patients";
import { ALLERGY_PERMISSIONS } from "@/lib/patient-permissions";
import { useAuth } from "@/auth/use-auth";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { EntityStatusBadge } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";
import { AllergyDialog } from "@/pages/patient-charts/allergy-dialog";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
};

export function AllergyListDialog({ patientId, open, onClose }: Props) {
  const { user } = useAuth();
  const queryClient = useQueryClient();

  const canCreate = user?.permissions?.includes(ALLERGY_PERMISSIONS.create) ?? false;
  const canUpdate = user?.permissions?.includes(ALLERGY_PERMISSIONS.update) ?? false;
  const canToggleActive = user?.permissions?.includes(ALLERGY_PERMISSIONS.delete) ?? false;

  const [showInactive, setShowInactive] = useState(false);
  const [editorOpen, setEditorOpen] = useState(false);
  const [editAllergy, setEditAllergy] = useState<PatientAllergy | null>(null);

  const allergiesQuery = useQuery({
    queryKey: ["patient-allergies", patientId, showInactive],
    queryFn: () =>
      searchPatientAllergies({ patientId, includeInactive: showInactive, pageSize: 200 }),
    enabled: open,
    placeholderData: keepPreviousData,
  });

  const patientQuery = useQuery({
    queryKey: ["patient", patientId],
    queryFn: () => getPatientById(patientId),
    enabled: open,
  });

  const allergies = useMemo(() => allergiesQuery.data?.items ?? [], [allergiesQuery.data]);
  const noKnownAllergies = patientQuery.data?.hasNoKnownAllergies ?? false;

  const noAllergiesMutation = useMutation({
    mutationFn: (value: boolean) => setNoKnownAllergies(patientId, value),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ["patient", patientId] });
    },
    onError: (err) => {
      // Legacy rule surfaces here as a 409 with the exact message.
      toast.warning("Could not change No Allergies.", { description: describe(err) });
      void queryClient.invalidateQueries({ queryKey: ["patient", patientId] });
    },
  });

  return (
    <>
      <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
        <DialogContent className="!max-w-3xl">
          <DialogHeader>
            <DialogTitle>Allergy List</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                <input
                  type="checkbox"
                  checked={showInactive}
                  onChange={(e) => setShowInactive(e.target.checked)}
                  className="rounded border-[var(--color-border)]"
                />
                <span>Show inactive</span>
              </label>
              <div className="flex items-center gap-3">
                <label className="flex items-center gap-2 text-[13px] cursor-pointer">
                  <input
                    type="checkbox"
                    checked={noKnownAllergies}
                    disabled={noAllergiesMutation.isPending}
                    onChange={(e) => noAllergiesMutation.mutate(e.target.checked)}
                    className="rounded border-[var(--color-border)]"
                  />
                  <span>Set No Allergies</span>
                </label>
                {canCreate && (
                  <Button
                    size="sm"
                    className="h-8 gap-1.5 rounded-lg px-3 text-[13px] font-semibold"
                    onClick={() => {
                      setEditAllergy(null);
                      setEditorOpen(true);
                    }}
                  >
                    <Plus className="size-4" />
                    Add Allergy
                  </Button>
                )}
              </div>
            </div>

            {allergiesQuery.isLoading ? (
              <div className="skeleton h-20 rounded-lg" />
            ) : allergies.length === 0 ? (
              <p className="py-6 text-center text-[13px] text-[var(--color-muted-foreground)]">
                {noKnownAllergies
                  ? "Patient marked as having no known allergies."
                  : "No allergies recorded for this patient."}
              </p>
            ) : (
              <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                {allergies.map((a) => (
                  <li key={a.id} className="flex items-center justify-between gap-3 px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium">{a.drugName}</p>
                      {(a.reaction || a.comments) && (
                        <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                          {[a.reaction, a.comments].filter(Boolean).join(" · ")}
                        </p>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="hidden text-[12px] text-[var(--color-muted-foreground)] sm:inline">
                        {formatDate(a.dateNoted)}
                      </span>
                      <EntityStatusBadge tone={a.isActive ? "success" : "warning"}>
                        {a.isActive ? "Active" : "Inactive"}
                      </EntityStatusBadge>
                      {canUpdate && (
                        <button
                          type="button"
                          title="Edit allergy"
                          aria-label="Edit allergy"
                          onClick={() => {
                            setEditAllergy(a);
                            setEditorOpen(true);
                          }}
                          className="inline-flex size-7 items-center justify-center rounded-md border border-[var(--color-border)] transition-colors hover:bg-[var(--color-accent)]"
                        >
                          <Pencil className="size-4" />
                        </button>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Close
              </Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <AllergyDialog
        patientId={patientId}
        open={editorOpen}
        onClose={() => {
          setEditorOpen(false);
          setEditAllergy(null);
        }}
        allergy={editAllergy}
        canToggleActive={canToggleActive}
      />
    </>
  );
}
```

- [ ] **Step 5: Chart button**

In `clients/dashboard/src/pages/patient-charts/chart.tsx`:

1. Imports: `Pill` from `lucide-react` (add to the existing lucide import), `{ AllergyListDialog } from "@/pages/patient-charts/allergy-list-dialog"`, and add `ALLERGY_PERMISSIONS` to the existing `@/lib/patient-permissions` import.
2. State next to `problemListOpen`:

```tsx
  const [allergyListOpen, setAllergyListOpen] = useState(false);
```

3. Permission next to `canViewProblems` (find its declaration and mirror):

```tsx
  const canViewAllergies = user?.permissions?.includes(ALLERGY_PERMISSIONS.view) ?? false;
```

4. Button in the chart-actions row (after the Problem List button, same classes):

```tsx
        {canViewAllergies && (
          <Button
            variant="outline"
            size="sm"
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            onClick={() => setAllergyListOpen(true)}
          >
            <Pill className="size-4" />
            Allergy List
          </Button>
        )}
```

5. Dialog mount next to the ProblemListDialog mount:

```tsx
      {patientId && allergyListOpen && (
        <AllergyListDialog
          patientId={patientId}
          open={allergyListOpen}
          onClose={() => setAllergyListOpen(false)}
        />
      )}
```

(Match exactly how the ProblemListDialog mount guards `patientId` — mirror its conditional shape.)

- [ ] **Step 6: Typecheck/lint + commit**

```bash
cd clients/dashboard && npx tsc -b --noEmit && npx eslint src/pages/patient-charts src/api/administration.ts
git add clients/dashboard/src
git commit -m "feat(dashboard): Allergy List dialog + chart shortcut button with drug/reaction pickers"
```

---

### Task 14: Frontend — Medication List dialog + reconciliation + chart button

**Files:**
- Create: `clients/dashboard/src/pages/patient-charts/medication-dialog.tsx`
- Create: `clients/dashboard/src/pages/patient-charts/medication-reconciliation-dialog.tsx`
- Create: `clients/dashboard/src/pages/patient-charts/medication-list-dialog.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx`

**Interfaces:**
- Consumes: Task 12 medication API, Task 13 `DrugPicker`, `listMedicationDoseUnits` (Task 13 Step 1).
- Produces: `<MedicationListDialog patientId open onClose />`.

- [ ] **Step 1: MedicationDialog (add/edit)**

`clients/dashboard/src/pages/patient-charts/medication-dialog.tsx` — same skeleton as `AllergyDialog`; fields per spec. Complete file:

```tsx
import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Info } from "lucide-react";
import { toast } from "sonner";
import {
  createMedication,
  medlinePlusUrl,
  updateMedication,
  type PatientMedication,
} from "@/api/medications";
import { listMedicationDoseUnits } from "@/api/administration";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Combobox, Field, type ComboboxOption } from "@/components/list";
import { describe } from "@/lib/list-helpers";
import { DrugPicker, type DrugSelection } from "@/pages/patient-charts/drug-picker";

/** Legacy dose-period units (PatientMedicationDetail.razor PeriodNames). */
const PERIOD_OPTIONS: ComboboxOption[] = [
  { value: "h", label: "h (hour)" },
  { value: "d", label: "d (day)" },
  { value: "wk", label: "wk (week)" },
  { value: "mo", label: "mo (month)" },
];

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  medication?: PatientMedication | null;
  canToggleActive: boolean;
};

export function MedicationDialog({ patientId, open, onClose, medication, canToggleActive }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!medication;

  const [drug, setDrug] = useState<DrugSelection | null>(null);
  const [prescriber, setPrescriber] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [doseValue, setDoseValue] = useState("");
  const [doseUnitId, setDoseUnitId] = useState<number | null>(null);
  const [dosePeriodValue, setDosePeriodValue] = useState("");
  const [dosePeriodUnit, setDosePeriodUnit] = useState<string | null>(null);
  const [instructions, setInstructions] = useState("");
  const [indication, setIndication] = useState("");
  const [isActive, setIsActive] = useState(true);

  useEffect(() => {
    if (!open) return;
    if (medication) {
      setDrug({
        name: medication.drugName,
        rxAui: medication.rxAui ?? null,
        rxCui: medication.rxCode ?? null,
      });
      setPrescriber(medication.prescriber ?? "");
      setStartDate(medication.startDate ? medication.startDate.slice(0, 10) : "");
      setEndDate(medication.endDate ? medication.endDate.slice(0, 10) : "");
      setDoseValue(medication.doseValue != null ? String(medication.doseValue) : "");
      setDoseUnitId(medication.doseUnitId ?? null);
      setDosePeriodValue(medication.dosePeriodValue != null ? String(medication.dosePeriodValue) : "");
      setDosePeriodUnit(medication.dosePeriodUnit ?? null);
      setInstructions(medication.instructions ?? "");
      setIndication(medication.indication ?? "");
      setIsActive(medication.isActive);
    } else {
      setDrug(null);
      setPrescriber("");
      setStartDate(new Date().toISOString().slice(0, 10));
      setEndDate("");
      setDoseValue("");
      setDoseUnitId(null);
      setDosePeriodValue("");
      setDosePeriodUnit(null);
      setInstructions("");
      setIndication("");
      setIsActive(true);
    }
  }, [open, medication]);

  const doseUnitsQuery = useQuery({
    queryKey: ["medication-dose-units", "active"],
    queryFn: () => listMedicationDoseUnits({ isActive: true }),
    enabled: open,
    staleTime: 5 * 60 * 1000,
  });

  const doseUnitOptions: ComboboxOption[] = (doseUnitsQuery.data ?? []).map((u) => ({
    value: String(u.id),
    label: u.name,
  }));

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-medications", patientId] });
    void queryClient.invalidateQueries({ queryKey: ["patient", patientId] });
  };

  const createMutation = useMutation({
    mutationFn: createMedication,
    onSuccess: () => {
      toast.success("Medication added.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to add medication.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateMedication,
    onSuccess: () => {
      toast.success("Medication updated.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to update medication.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!drug?.name || !startDate) return;
    const fields = {
      drugName: drug.name,
      rxAui: drug.rxAui,
      rxCode: drug.rxCui,
      ndc: medication?.ndc ?? null,
      prescriber: prescriber.trim() || null,
      startDate,
      endDate: endDate || null,
      doseValue: doseValue ? Number(doseValue) : null,
      doseUnitId,
      dosePeriodValue: dosePeriodValue ? Number(dosePeriodValue) : null,
      dosePeriodUnit,
      instructions: instructions.trim() || null,
      indication: indication.trim() || null,
      isActive,
    };
    if (isEdit && medication) {
      updateMutation.mutate({ ...fields, medicationId: medication.id });
    } else {
      createMutation.mutate({ ...fields, patientId });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle className="flex items-center justify-between gap-2">
              {isEdit ? "Edit Medication" : "Add Medication"}
              {medication?.rxCode && (
                <a
                  href={medlinePlusUrl(medication.rxCode)}
                  target="_blank"
                  rel="noreferrer"
                  title="Drug info (MedlinePlus)"
                  className="inline-flex items-center gap-1 text-[12px] font-medium text-[var(--color-primary)] hover:underline"
                >
                  <Info className="size-4" />
                  Info
                </a>
              )}
            </DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <DrugPicker value={drug} onChange={setDrug} label="Medication" />

            <Field id="med-prescriber" label="Prescriber">
              <Input
                id="med-prescriber"
                type="text"
                value={prescriber}
                maxLength={256}
                onChange={(e) => setPrescriber(e.target.value)}
              />
            </Field>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="med-start" label="Start Date">
                <Input
                  id="med-start"
                  type="date"
                  value={startDate}
                  onChange={(e) => setStartDate(e.target.value)}
                  required
                />
              </Field>
              <Field id="med-end" label="End Date">
                <Input
                  id="med-end"
                  type="date"
                  value={endDate}
                  onChange={(e) => setEndDate(e.target.value)}
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="med-dose-value" label="Dosage">
                <Input
                  id="med-dose-value"
                  type="number"
                  min="0"
                  step="any"
                  value={doseValue}
                  onChange={(e) => setDoseValue(e.target.value)}
                />
              </Field>
              <Field id="med-dose-unit" label="Dose Unit">
                <Combobox
                  id="med-dose-unit"
                  label="Dose Unit"
                  value={doseUnitId != null ? String(doseUnitId) : null}
                  onChange={(v) => setDoseUnitId(v ? Number(v) : null)}
                  options={doseUnitOptions}
                  placeholder="Select…"
                />
              </Field>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <Field id="med-period-value" label="Dosage Period">
                <Input
                  id="med-period-value"
                  type="number"
                  min="0"
                  step="any"
                  value={dosePeriodValue}
                  onChange={(e) => setDosePeriodValue(e.target.value)}
                />
              </Field>
              <Field id="med-period-unit" label="Period Unit">
                <Combobox
                  id="med-period-unit"
                  label="Period Unit"
                  value={dosePeriodUnit}
                  onChange={setDosePeriodUnit}
                  options={PERIOD_OPTIONS}
                  placeholder="Select…"
                />
              </Field>
            </div>

            <Field id="med-instructions" label="Special Instructions">
              <Textarea
                id="med-instructions"
                value={instructions}
                onChange={(e) => setInstructions(e.target.value)}
                rows={2}
                maxLength={4000}
              />
            </Field>

            <Field id="med-indication" label="Indications">
              <Textarea
                id="med-indication"
                value={indication}
                onChange={(e) => setIndication(e.target.value)}
                rows={2}
                maxLength={4000}
              />
            </Field>

            {canToggleActive && (
              <div className="flex items-center gap-4 text-[13px]">
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="med-active"
                    checked={isActive}
                    onChange={() => setIsActive(true)}
                  />
                  <span>Active</span>
                </label>
                <label className="flex items-center gap-2 cursor-pointer">
                  <input
                    type="radio"
                    name="med-active"
                    checked={!isActive}
                    onChange={() => setIsActive(false)}
                  />
                  <span>Inactive</span>
                </label>
              </div>
            )}

            {isEdit && medication?.createdByName && (
              <p className="text-[12px] text-[var(--color-muted-foreground)]">
                Created: {medication.createdByName}
                {medication.updatedByName ? ` · Last modified: ${medication.updatedByName}` : ""}
              </p>
            )}
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !drug?.name || !startDate}>
              {isPending ? "Saving…" : isEdit ? "Save Changes" : "Add Medication"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 2: MedicationReconciliationDialog**

`clients/dashboard/src/pages/patient-charts/medication-reconciliation-dialog.tsx`:

```tsx
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  getMedicationReconciledDates,
  markMedicationsReconciled,
  type PatientMedication,
} from "@/api/medications";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { EntityStatusBadge } from "@/components/list";
import { describe, formatDate } from "@/lib/list-helpers";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  medications: PatientMedication[];
  canReconcile: boolean;
};

/** Legacy Reconciliation dialog, minus CCD import (a stub even in BackChart):
 *  current meds on the left, "Mark Reconciled Today" + Dates Reconciled history on the right. */
export function MedicationReconciliationDialog({
  patientId,
  open,
  onClose,
  medications,
  canReconcile,
}: Props) {
  const queryClient = useQueryClient();

  const datesQuery = useQuery({
    queryKey: ["medication-reconciled-dates", patientId],
    queryFn: () => getMedicationReconciledDates(patientId),
    enabled: open,
  });

  const markMutation = useMutation({
    mutationFn: () => markMedicationsReconciled(patientId),
    onSuccess: () => {
      toast.success("Medications marked reconciled.");
      void queryClient.invalidateQueries({ queryKey: ["medication-reconciled-dates", patientId] });
    },
    onError: (err) => toast.error("Failed to mark reconciled.", { description: describe(err) }),
  });

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-3xl">
        <DialogHeader>
          <DialogTitle>Medication Reconciliation</DialogTitle>
        </DialogHeader>

        <DialogBody>
          <div className="grid gap-4 sm:grid-cols-2">
            <div>
              <h3 className="mb-2 text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                Current Medications
              </h3>
              {medications.length === 0 ? (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">No medications.</p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {medications.map((m) => (
                    <li key={m.id} className="flex items-center justify-between gap-2 px-3 py-2">
                      <div className="min-w-0">
                        <p className="truncate text-[13px]">{m.drugName}</p>
                        <p className="text-[12px] text-[var(--color-muted-foreground)]">
                          {formatDate(m.startDate)}
                          {m.rxCode ? ` · Rx ${m.rxCode}` : ""}
                        </p>
                      </div>
                      <EntityStatusBadge tone={m.isActive ? "success" : "warning"}>
                        {m.isActive ? "Active" : "Inactive"}
                      </EntityStatusBadge>
                    </li>
                  ))}
                </ul>
              )}
            </div>

            <div>
              <div className="mb-2 flex items-center justify-between">
                <h3 className="text-[12px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
                  Dates Reconciled
                </h3>
                {canReconcile && (
                  <Button
                    size="sm"
                    className="h-8 rounded-lg px-3 text-[13px] font-semibold"
                    disabled={markMutation.isPending}
                    onClick={() => markMutation.mutate()}
                  >
                    Mark Reconciled Today
                  </Button>
                )}
              </div>
              {datesQuery.isLoading ? (
                <div className="skeleton h-16 rounded-lg" />
              ) : (datesQuery.data ?? []).length === 0 ? (
                <p className="text-[13px] text-[var(--color-muted-foreground)]">
                  No reconciliation recorded yet.
                </p>
              ) : (
                <ul className="divide-y divide-[var(--color-border)] rounded-lg border border-[var(--color-border)]">
                  {(datesQuery.data ?? []).map((d) => (
                    <li key={d.id} className="px-3 py-2 text-[13px]">
                      {formatDate(d.reconciledOn)}
                      {d.createdByName && (
                        <span className="text-[12px] text-[var(--color-muted-foreground)]">
                          {" "}— {d.createdByName}
                        </span>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </div>
        </DialogBody>

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Close
            </Button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 3: MedicationListDialog**

`clients/dashboard/src/pages/patient-charts/medication-list-dialog.tsx` — same skeleton as `AllergyListDialog` with: query key `["patient-medications", patientId, showInactive]` via `searchPatientMedications`; `MEDICATION_PERMISSIONS`; "Set No Medications" checkbox calling `setNoKnownMedications` (flag field `patientQuery.data?.hasNoKnownMedications`); a **Reconciliation** button next to Add Medication opening `MedicationReconciliationDialog` (pass `medications` from the current query result and `canReconcile={canUpdate}`); row content:

```tsx
                    <div className="min-w-0">
                      <p className="truncate text-[13px] font-medium">{m.drugName}</p>
                      <p className="truncate text-[12px] text-[var(--color-muted-foreground)]">
                        {[
                          m.doseValue != null ? `${m.doseValue}${m.dosePeriodUnit ? "/" + m.dosePeriodUnit : ""}` : null,
                          m.prescriber,
                        ]
                          .filter(Boolean)
                          .join(" · ")}
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="hidden text-[12px] text-[var(--color-muted-foreground)] sm:inline">
                        {formatDate(m.startDate)}
                        {m.endDate ? ` – ${formatDate(m.endDate)}` : ""}
                      </span>
                      <EntityStatusBadge tone={m.isActive ? "success" : "warning"}>
                        {m.isActive ? "Active" : "Inactive"}
                      </EntityStatusBadge>
                      {/* edit pencil identical to allergy list */}
                    </div>
```

and mounts `<MedicationDialog …/>` + `<MedicationReconciliationDialog …/>` after the main Dialog. Write the full file by transposing `allergy-list-dialog.tsx` — every allergy identifier → medication equivalent, plus the two additions above (reconciliation button + dialog). Empty-state text: "Patient marked as having no known medications." / "No medications recorded for this patient."

- [ ] **Step 4: Chart button**

In `chart.tsx` (mirror Task 13 Step 5): lucide icon `Pill` is taken — use `Tablets` for medications (import it); state `medicationListOpen`; permission `canViewMedications` from `MEDICATION_PERMISSIONS.view`; button labeled `Medication List` after the Allergy List button; dialog mount `<MedicationListDialog …/>`.

- [ ] **Step 5: Typecheck/lint + commit**

```bash
cd clients/dashboard && npx tsc -b --noEmit && npx eslint src/pages/patient-charts
git add clients/dashboard/src
git commit -m "feat(dashboard): Medication List dialog + reconciliation + chart shortcut button"
```

---

### Task 15: Frontend — Patient Notes dialog + chart button + medical-alert banner

**Files:**
- Create: `clients/dashboard/src/pages/patient-charts/patient-note-dialog.tsx`
- Create: `clients/dashboard/src/pages/patient-charts/patient-notes-dialog.tsx`
- Modify: `clients/dashboard/src/pages/patient-charts/chart.tsx`

**Interfaces:**
- Consumes: Task 12 notes API, `NOTE_PERMISSIONS`.
- Produces: `<PatientNotesDialog patientId open onClose />`; chart Medical Alerts banner now includes alert-flagged notes.

- [ ] **Step 1: PatientNoteDialog (add/edit)**

`clients/dashboard/src/pages/patient-charts/patient-note-dialog.tsx`:

```tsx
import { useEffect, useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { createNote, updateNote, type PatientNote } from "@/api/patient-notes";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Field } from "@/components/list";
import { describe } from "@/lib/list-helpers";

type Props = {
  patientId: string;
  open: boolean;
  onClose(): void;
  note?: PatientNote | null;
};

export function PatientNoteDialog({ patientId, open, onClose, note }: Props) {
  const queryClient = useQueryClient();
  const isEdit = !!note;

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isMedicalAlert, setIsMedicalAlert] = useState(false);

  useEffect(() => {
    if (!open) return;
    setName(note?.name ?? "");
    setDescription(note?.description ?? "");
    setIsMedicalAlert(note?.isMedicalAlert ?? false);
  }, [open, note]);

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ["patient-notes", patientId] });
  };

  const createMutation = useMutation({
    mutationFn: createNote,
    onSuccess: () => {
      toast.success("Patient note saved.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to save note.", { description: describe(err) }),
  });

  const updateMutation = useMutation({
    mutationFn: updateNote,
    onSuccess: () => {
      toast.success("Patient note saved.");
      invalidate();
      onClose();
    },
    onError: (err) => toast.error("Failed to save note.", { description: describe(err) }),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!name.trim()) return;
    if (isEdit && note) {
      updateMutation.mutate({
        noteId: note.id,
        name: name.trim(),
        description: description.trim() || null,
        isMedicalAlert,
      });
    } else {
      createMutation.mutate({
        patientId,
        name: name.trim(),
        description: description.trim() || null,
        isMedicalAlert,
      });
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-lg">
        <form onSubmit={onSubmit}>
          <DialogHeader>
            <DialogTitle>{isEdit ? "Edit Patient Note" : "Add Patient Note"}</DialogTitle>
          </DialogHeader>

          <DialogBody className="space-y-4">
            <Field id="note-name" label="Name">
              <Input
                id="note-name"
                type="text"
                value={name}
                maxLength={256}
                onChange={(e) => setName(e.target.value)}
                required
                autoFocus
              />
            </Field>

            <Field id="note-description" label="Description">
              <Textarea
                id="note-description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={4}
                maxLength={8000}
              />
            </Field>

            <label className="flex w-fit items-center gap-2 text-[13px] cursor-pointer">
              <input
                type="checkbox"
                checked={isMedicalAlert}
                onChange={(e) => setIsMedicalAlert(e.target.checked)}
                className="rounded border-[var(--color-border)]"
              />
              <span>Flag as medical alert</span>
            </label>
          </DialogBody>

          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" disabled={isPending}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isPending || !name.trim()}>
              {isPending ? "Saving…" : "Save Note"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 2: PatientNotesDialog (list)**

`clients/dashboard/src/pages/patient-charts/patient-notes-dialog.tsx` — transpose `allergy-list-dialog.tsx`: query key `["patient-notes", patientId]` via `searchPatientNotes({ patientId, pageSize: 200 })` (no Show-Inactive/No-flag controls, no patient query); permissions from `NOTE_PERMISSIONS` (`canCreate`/`canUpdate`/`canDelete`); "Add Note" button opens `PatientNoteDialog` with `note=null`; row shows `n.name` (with `AlertTriangle` icon when `n.isMedicalAlert`, like `ProblemListDialog` rows), truncated `n.description`, `formatDate(n.createdAtUtc)`; edit pencil (canUpdate) opens the editor; delete trash (canDelete) runs:

```tsx
  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteNote(id),
    onSuccess: () => {
      toast.success("Note deleted.");
      void queryClient.invalidateQueries({ queryKey: ["patient-notes", patientId] });
    },
    onError: (err) => toast.error("Failed to delete note.", { description: describe(err) }),
  });
```

(delete button identical markup to `ProblemListDialog`'s trash button). Empty state: "No notes recorded for this patient." Mount `<PatientNoteDialog patientId={patientId} open={editorOpen} onClose={...} note={editNote} />` after the main Dialog.

- [ ] **Step 3: Chart button + alerts banner**

In `chart.tsx`:

1. Imports: `StickyNote` from lucide, `PatientNotesDialog`, `NOTE_PERMISSIONS`, and `searchPatientNotes` from `@/api/patient-notes`.
2. State `notesOpen`; permission `canViewNotes` mirroring the others.
3. Button after Medication List:

```tsx
        {canViewNotes && (
          <Button
            variant="outline"
            size="sm"
            className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
            onClick={() => setNotesOpen(true)}
          >
            <StickyNote className="size-4" />
            Patient Notes
          </Button>
        )}
```

4. Alert notes query next to the existing `medicalAlertsQuery` (~line 272):

```tsx
  const noteAlertsQuery = useQuery({
    queryKey: ["patient-notes", patientId, "alerts"],
    queryFn: () =>
      searchPatientNotes({ patientId: patientId!, medicalAlertsOnly: true, pageSize: 100 }),
    enabled: !!patientId && canViewNotes,
  });

  const medicalAlertNotes = useMemo(
    () => noteAlertsQuery.data?.items ?? [],
    [noteAlertsQuery.data],
  );
```

5. Extend the existing Medical Alerts banner (~line 337): change its render condition to `(canViewProblems && medicalAlertProblems.length > 0) || (canViewNotes && medicalAlertNotes.length > 0)` and, inside the banner's list, render note entries after the problem entries (match the problem entries' exact markup — open the banner JSX and mirror its `<li>` shape):

```tsx
                {canViewNotes &&
                  medicalAlertNotes.map((n) => (
                    <li key={n.id}>
                      {n.name}
                      {n.description ? ` — ${n.description}` : ""}
                    </li>
                  ))}
```

6. Dialog mount:

```tsx
      {patientId && notesOpen && (
        <PatientNotesDialog patientId={patientId} open={notesOpen} onClose={() => setNotesOpen(false)} />
      )}
```

- [ ] **Step 4: Typecheck/lint + commit**

```bash
cd clients/dashboard && npx tsc -b --noEmit && npx eslint src/pages/patient-charts
git add clients/dashboard/src
git commit -m "feat(dashboard): Patient Notes dialog + chart shortcut + notes in medical-alert banner"
```

---

### Task 16: Frontend — admin pages (Drugs, Allergy Reactions, Dose Units) + routes + nav

**Files:**
- Modify: `clients/dashboard/src/api/administration.ts` (add drug CRUD + RxNav + reaction/dose-unit CRUD functions)
- Create: `clients/dashboard/src/pages/administration/drugs.tsx`
- Create: `clients/dashboard/src/pages/administration/allergy-reactions.tsx`
- Create: `clients/dashboard/src/pages/administration/medication-dose-units.tsx`
- Modify: `clients/dashboard/src/routes.tsx` (3 lazy pages + 3 routes)
- Modify: `clients/dashboard/src/components/layout/nav-data.ts` (3 nav items)

**Interfaces:**
- Consumes: Task 7–9 endpoints; Task 13's `DrugDto`/`AllergyReactionDto`/`MedicationDoseUnitDto` + list functions already in `administration.ts`.
- Produces: `DrugsPage`, `AllergyReactionsPage`, `MedicationDoseUnitsPage` (named exports, lazy-routed).

- [ ] **Step 1: administration.ts CRUD + RxNav functions**

Append (next to the Task 13 list functions):

```ts
export type DrugInput = {
  name: string;
  rxAui?: string | null;
  rxCui?: string | null;
  tty?: string | null;
  sab?: string | null;
  code?: string | null;
};

function drugBody(input: DrugInput): Record<string, unknown> {
  return {
    name: input.name,
    rxAui: input.rxAui ?? null,
    rxCui: input.rxCui ?? null,
    tty: input.tty ?? null,
    sab: input.sab ?? null,
    code: input.code ?? null,
  };
}

export async function createDrug(input: DrugInput): Promise<number> {
  return apiFetch<number>("/api/v1/administration/drugs", {
    method: "POST",
    body: JSON.stringify(drugBody(input)),
  });
}

export async function updateDrug(input: DrugInput & { id: number; isActive: boolean }): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/drugs/${input.id}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.id, ...drugBody(input), isActive: input.isActive }),
  });
}

export async function deleteDrug(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/drugs/${id}`, { method: "DELETE" });
}

export type RxNavDrug = { rxCui: string; name: string; tty?: string | null };

export function searchRxNav(term: string): Promise<RxNavDrug[]> {
  const query = new URLSearchParams({ term });
  return apiFetch<RxNavDrug[]>(`/api/v1/administration/drugs/rxnav?${query.toString()}`);
}

export async function importDrugs(items: RxNavDrug[]): Promise<number> {
  return apiFetch<number>("/api/v1/administration/drugs/import", {
    method: "POST",
    body: JSON.stringify({ items }),
  });
}

export async function createAllergyReaction(input: { term: string; snomedCode?: string | null }): Promise<number> {
  return apiFetch<number>("/api/v1/administration/allergy-reactions", {
    method: "POST",
    body: JSON.stringify({ term: input.term, snomedCode: input.snomedCode ?? null }),
  });
}

export async function updateAllergyReaction(input: {
  id: number;
  term: string;
  snomedCode?: string | null;
  isActive: boolean;
}): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/allergy-reactions/${input.id}`, {
    method: "PUT",
    body: JSON.stringify({
      id: input.id,
      term: input.term,
      snomedCode: input.snomedCode ?? null,
      isActive: input.isActive,
    }),
  });
}

export async function deleteAllergyReaction(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/allergy-reactions/${id}`, { method: "DELETE" });
}

export async function createMedicationDoseUnit(input: { name: string }): Promise<number> {
  return apiFetch<number>("/api/v1/administration/medication-dose-units", {
    method: "POST",
    body: JSON.stringify({ name: input.name }),
  });
}

export async function updateMedicationDoseUnit(input: {
  id: number;
  name: string;
  isActive: boolean;
}): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/medication-dose-units/${input.id}`, {
    method: "PUT",
    body: JSON.stringify({ id: input.id, name: input.name, isActive: input.isActive }),
  });
}

export async function deleteMedicationDoseUnit(id: number): Promise<void> {
  await apiFetch<void>(`/api/v1/administration/medication-dose-units/${id}`, { method: "DELETE" });
}
```

- [ ] **Step 2: Allergy Reactions page**

`clients/dashboard/src/pages/administration/allergy-reactions.tsx` — transpose `code-sources.tsx` (the smallest admin page, 376 lines) field-for-field. Concretely:

- Component: `export function AllergyReactionsPage()`.
- Query: `useQuery({ queryKey: ["administration", "allergy-reactions-page"], queryFn: () => listAllergyReactions() })`, client-side search filter on `term`.
- Header: icon `AlertTriangle` (lucide), title "Allergy Reactions", description "Curated SNOMED reaction options offered by the patient chart's allergy dialog.", unit "reaction".
- Desktop grid `grid-cols-[1fr_140px_90px_24px]`: Term · SNOMED code · status badge · chevron; mobile card mirrors `code-sources.tsx`'s `MobileCard`.
- Create/edit dialog fields: Term (`Input`, required, maxLength 256), SNOMED Code (`Input`, maxLength 32), Active (`Switch`, edit mode only).
- Mutations call `createAllergyReaction` / `updateAllergyReaction` / `deleteAllergyReaction`, invalidating `["administration", "allergy-reactions-page"]` and `["allergy-reactions"]` (the chart picker key), with the same `EditorState` union + delete-confirm dialog as `code-sources.tsx`.

- [ ] **Step 3: Medication Dose Units page**

`clients/dashboard/src/pages/administration/medication-dose-units.tsx` — same transposition: `export function MedicationDoseUnitsPage()`, icon `Beaker`, title "Medication Dose Units", unit "unit", single Name field (maxLength 64) + Active switch, functions `listMedicationDoseUnits`/`createMedicationDoseUnit`/`updateMedicationDoseUnit`/`deleteMedicationDoseUnit`, invalidating `["administration", "medication-dose-units-page"]` and `["medication-dose-units"]`.

- [ ] **Step 4: Drugs page**

`clients/dashboard/src/pages/administration/drugs.tsx` — transpose `diagnostics.tsx` (the paginated-search admin page) since Drugs is server-paginated. `export function DrugsPage()`:

- Server-side search: `useQuery({ queryKey: ["administration", "drugs-page", { search, pageNumber }], queryFn: () => listDrugs({ search, pageNumber, pageSize: 20 }), placeholderData: keepPreviousData })` with the same debounced `EntitySearch` + pagination controls diagnostics.tsx uses (copy its pagination JSX).
- Header: icon `Pill`, title "Drugs", description "RxNorm-backed drug catalog used by the patient chart's allergy and medication pickers.", plus TWO header buttons: "New drug" (create dialog) and "Import from RxNav" (import dialog).
- Row grid `grid-cols-[1fr_110px_90px_90px_24px]`: Name · RxCUI · TTY · status · chevron.
- Create/edit dialog fields: Name (required, maxLength 2048), RxAUI, RxCUI, TTY, SAB, Code (all optional `Input`s), Active switch on edit. Delete uses the standard confirm dialog.
- **RxNav import dialog** (inside the same file):

```tsx
function RxNavImportDialog({ open, onClose }: { open: boolean; onClose(): void }) {
  const queryClient = useQueryClient();
  const [term, setTerm] = useState("");
  const [submittedTerm, setSubmittedTerm] = useState("");
  const [selected, setSelected] = useState<Record<string, RxNavDrug>>({});

  const rxNavQuery = useQuery({
    queryKey: ["administration", "rxnav-search", submittedTerm],
    queryFn: () => searchRxNav(submittedTerm),
    enabled: open && submittedTerm.length >= 3,
  });

  const importMutation = useMutation({
    mutationFn: (items: RxNavDrug[]) => importDrugs(items),
    onSuccess: (count) => {
      toast.success(`Imported ${count} drug${count === 1 ? "" : "s"} from RxNav.`);
      setSelected({});
      void queryClient.invalidateQueries({ queryKey: ["administration", "drugs-page"] });
      void queryClient.invalidateQueries({ queryKey: ["drug-search"] });
      onClose();
    },
    onError: (err) => toast.error("Import failed.", { description: describe(err) }),
  });

  const results = rxNavQuery.data ?? [];
  const selectedList = Object.values(selected);

  return (
    <Dialog open={open} onOpenChange={(o) => (!o ? onClose() : undefined)}>
      <DialogContent className="!max-w-2xl">
        <DialogHeader>
          <DialogTitle>Import from RxNav</DialogTitle>
          <DialogDescription>
            Search the NIH RxNorm API and choose which concepts to add to the local catalog.
          </DialogDescription>
        </DialogHeader>
        <DialogBody className="space-y-3">
          <form
            className="flex gap-2"
            onSubmit={(e) => {
              e.preventDefault();
              setSubmittedTerm(term.trim());
            }}
          >
            <Input
              value={term}
              onChange={(e) => setTerm(e.target.value)}
              placeholder="Drug name (min 3 chars)…"
              autoFocus
            />
            <Button type="submit" disabled={term.trim().length < 3 || rxNavQuery.isFetching}>
              {rxNavQuery.isFetching ? "Searching…" : "Search"}
            </Button>
          </form>

          {rxNavQuery.isError && (
            <p className="text-[13px] text-[var(--color-destructive)]">
              RxNav search failed. {describe(rxNavQuery.error)}
            </p>
          )}

          {results.length > 0 && (
            <ul className="max-h-72 divide-y divide-[var(--color-border)] overflow-y-auto rounded-lg border border-[var(--color-border)]">
              {results.map((d) => (
                <li key={d.rxCui} className="flex items-center gap-2 px-3 py-2">
                  <input
                    type="checkbox"
                    checked={!!selected[d.rxCui]}
                    onChange={(e) =>
                      setSelected((prev) => {
                        const next = { ...prev };
                        if (e.target.checked) next[d.rxCui] = d;
                        else delete next[d.rxCui];
                        return next;
                      })
                    }
                    className="rounded border-[var(--color-border)]"
                  />
                  <div className="min-w-0">
                    <p className="truncate text-[13px]">{d.name}</p>
                    <p className="text-[12px] text-[var(--color-muted-foreground)]">
                      RxCUI {d.rxCui}
                      {d.tty ? ` · ${d.tty}` : ""}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          )}

          {submittedTerm && !rxNavQuery.isFetching && results.length === 0 && !rxNavQuery.isError && (
            <p className="text-[13px] text-[var(--color-muted-foreground)]">No matches.</p>
          )}
        </DialogBody>
        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancel
            </Button>
          </DialogClose>
          <Button
            type="button"
            disabled={selectedList.length === 0 || importMutation.isPending}
            onClick={() => importMutation.mutate(selectedList)}
          >
            {importMutation.isPending ? "Importing…" : `Import ${selectedList.length || ""}`.trim()}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

- [ ] **Step 5: Routes + nav**

`clients/dashboard/src/routes.tsx` — with the other administration lazyNamed declarations:

```tsx
const DrugsPage = lazyNamed(() => import("@/pages/administration/drugs"), "DrugsPage");
const AllergyReactionsPage = lazyNamed(
  () => import("@/pages/administration/allergy-reactions"),
  "AllergyReactionsPage",
);
const MedicationDoseUnitsPage = lazyNamed(
  () => import("@/pages/administration/medication-dose-units"),
  "MedicationDoseUnitsPage",
);
```

and routes next to the other administration routes:

```tsx
          { path: "administration/drugs", element: withSuspense(<DrugsPage />) },
          { path: "administration/allergy-reactions", element: withSuspense(<AllergyReactionsPage />) },
          { path: "administration/medication-dose-units", element: withSuspense(<MedicationDoseUnitsPage />) },
```

`clients/dashboard/src/components/layout/nav-data.ts` — after the Diagnostic Details item (import `Pill`, `AlertTriangle`, `Beaker` from lucide at the top of the file if absent):

```ts
      {
        to: "/administration/drugs",
        label: "Drugs",
        icon: Pill,
        perm: "Permissions.Administration.Drugs.View",
      },
      {
        to: "/administration/allergy-reactions",
        label: "Allergy Reactions",
        icon: AlertTriangle,
        perm: "Permissions.Administration.AllergyReactions.View",
      },
      {
        to: "/administration/medication-dose-units",
        label: "Medication Dose Units",
        icon: Beaker,
        perm: "Permissions.Administration.MedicationDoseUnits.View",
      },
```

- [ ] **Step 6: Typecheck/lint + commit**

```bash
cd clients/dashboard && npx tsc -b --noEmit && npx eslint src
git add clients/dashboard/src
git commit -m "feat(dashboard): admin pages for Drugs (with RxNav import), Allergy Reactions, Dose Units"
```

---

### Task 17: Final verification + demo-role permissions + memory

**Files:**
- Possibly modify: `src/Host/FSH.Starter.DbMigrator/DemoSeed/DemoSeeder.cs` (role permission lists)
- Modify: `C:\Users\fcoyo\.claude\projects\c--Users-fcoyo-source-repos-clinic-solution-app\memory\project_patient_sprints.md` (progress note)

- [ ] **Step 1: Demo roles**

Open `DemoSeeder.cs` and check how Manager/Support roles received Problems/Incidents permissions (the Sprint 2 fix mentioned in memory). Add the new `Patient.Allergies/Medications/Notes` and `Administration.Drugs/AllergyReactions/MedicationDoseUnits` permission strings to the same role lists, mirroring the Problems entries. If roles are derived from `IsBasic` flags automatically, verify no change is needed and note it.

- [ ] **Step 2: Full verification suite**

```bash
dotnet build src/FSH.Starter.slnx
dotnet test src/FSH.Starter.slnx --filter "FullyQualifiedName!~Integration" --no-restore
cd clients/dashboard && npx tsc -b --noEmit && npx eslint src && cd ../..
```

Expected: all green, 0 warnings. (Integration.Tests need Docker; run them too if Docker is up.)

- [ ] **Step 3: Manual smoke (requires local stack)**

Start the API + dashboard (`dotnet run --project src/Host/FSH.Starter.Api` + `cd clients/dashboard && npm run dev`), open a patient chart and verify:

1. Three new buttons render next to Problem List (given permissions).
2. Allergy List: add allergy with drug search + reaction append; Set No Allergies while an active allergy exists → warning toast with the legacy message; deactivate the allergy → Set No Allergies succeeds; add active allergy again → flag clears (re-open dialog, checkbox unchecked).
3. Medication List: add medication with dose fields; Info link appears when the picked drug has an RxCUI; Reconciliation → Mark Reconciled Today adds a dated row.
4. Patient Notes: add alert-flagged note → it appears in the chart's Medical Alerts banner; delete removes it from the list and banner.
5. Administration: Drugs page searches/creates; RxNav import round-trips (needs internet); Allergy Reactions + Dose Units CRUD work; chart pickers reflect admin edits after cache invalidation.

- [ ] **Step 4: Update memory + commit**

Append to the memory file's sprint status: Allergy/Medication/Notes feature complete on `clinic-app` (chart dialogs + Administration drug catalog + RxNav import + migrator verbs), with any RE-VERIFY items actually confirmed against the live legacy DB noted as done/pending.

```bash
git add -A
git commit -m "chore(patient): final verification pass for allergy/medication/notes feature"
```

(Skip the commit if nothing changed in Steps 1–3.)

---

## Plan self-review notes (author)

- **Spec coverage:** chart buttons (13–15), Patient aggregates + slices (1–4), No-flags + 409 rules (1, 2, 5), permissions (1–3, 7–8), Drug/Reaction/DoseUnit catalogs + admin CRUD (7–8, 16), RxNav on-demand sync (9, 16), DbMigrator verbs (10–11), API modules + dialogs + pickers (12–15), reconciliation dialog with Mark-Reconciled-Today + history (4, 14), medical-alert notes in chart banner (15), unit tests per handler (each backend task), Playwright deferred (spec §6 — no task, intentional).
- **Known verify-at-implementation points (flagged inline):** `PatientDemographics/Contact/Phi.Create` factory signatures (Task 1 tests), `CustomException.StatusCode` property (Task 5), `AdministrationDbContext` configuration-registration style (Task 7), `HasData` idiom (Task 8), `InternalsVisibleTo` for Administration.Tests (Task 9), Drugs COPY column list vs generated migration (Task 10), legacy `PatientMedications` column names (Task 11), ProblemListDialog mount guard shape (Task 13), Medical Alerts banner `<li>` markup (Task 15). (The `hasNoKnownAllergies`/`hasNoKnownMedications` fields on `getPatientById`'s type were confirmed present in `clients/dashboard/src/api/patients.ts:155-156`.)
- **Type consistency:** command/DTO/type names cross-checked across tasks (`PatientAllergyDto` et al. in 1→12→13; `MedicationFields` in 12→14; `RxNavDrugDto`/`RxNavDrug` backend/frontend pair in 9→16; `DrugSelection` in 13→14).
