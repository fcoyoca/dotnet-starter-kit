# Auto-Generated Sequential Patient Codes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** New patients get a server-assigned `P-<number>` code from a global Postgres sequence instead of a hand-typed code; the manual MSSQL migration path is untouched.

**Architecture:** A Postgres SEQUENCE (`PatientCodeSequence`, starts at 100,000) lives in the Patient module's EF model. `CreatePatientCommand.PatientCode` becomes optional — when absent, a small `IPatientCodeGenerator` seam calls `nextval()` via raw ADO and formats `P-<n>`; when present (the MSSQL migration importer), it's used as-is. A new read-only preview endpoint peeks the sequence (without consuming it) so the "Register a patient" dialog can show a best-effort next-code preview.

**Tech Stack:** .NET 10 / EF Core 10 / Npgsql / Mediator (backend); React 19 + TanStack Query v5 (dashboard); xUnit + NSubstitute + EF Core InMemory (backend unit tests); Playwright (E2E).

## Global Constraints

- Mediator handlers: `public sealed`, return `ValueTask<T>`, `.ConfigureAwait(false)` on every await, propagate `CancellationToken`.
- Build runs with `TreatWarningsAsErrors` — warnings fail the build. File-scoped namespaces, explicit types, `is null`/`is not null`.
- No `src/BuildingBlocks` changes. No cross-module references.
- **The MSSQL migration importer (`MssqlPatientMapper.cs`) must not change** — it keeps supplying its own `PatientCode: $"P-{pId}"` and must continue to work exactly as today.
- Sequence starts at **100,000**, increments by 1, lives in the `patient` schema, named `PatientCodeSequence`.
- Format is always `P-<number>` — no zero-padding.
- Preview endpoint is gated by `PatientPermissions.Patients.Create` (same as `CreatePatient`).
- Frontend: pass per-call data through `mutate(arg)` / query params, never via state the mutation callbacks close over.
- Spec: `docs/superpowers/specs/2026-07-01-patient-code-auto-generation-design.md`.

---

### Task 1: Add the PatientCodeSequence to the EF model + migration

**Files:**
- Modify: `src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs`
- Create: EF migration under `src/Host/FSH.Starter.Migrations.PostgreSQL/Patient/` (via tooling, not hand-written)

**Interfaces:**
- Produces: a Postgres sequence `"patient"."PatientCodeSequence"` (bigint, start 100000, increment 1), queryable via `SELECT nextval(...)` / `SELECT last_value, is_called FROM ...`.

- [ ] **Step 1: Add the sequence to `OnModelCreating`**

In `PatientDbContext.cs`, inside `OnModelCreating` (the method ending with
`base.OnModelCreating(modelBuilder);`), add the sequence declaration
immediately after `modelBuilder.HasDefaultSchema(Schema);`:

```csharp
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasSequence<long>("PatientCodeSequence", schema: Schema)
            .StartsAt(100_000)
            .IncrementsBy(1);
        // PatientConfiguration requires IPhiEncryptor — apply it directly instead of via reflection
        modelBuilder.ApplyConfiguration(new PatientConfiguration(_phi));
        modelBuilder.ApplyConfiguration(new PatientIncidentConfiguration());
        modelBuilder.ApplyConfiguration(new PatientIncidentDiagnosticConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportFieldValueConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportAddendumConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportProblemConfiguration());
        modelBuilder.ApplyConfiguration(new PatientProblemConfiguration());
        base.OnModelCreating(modelBuilder);
    }
```

- [ ] **Step 2: Build first (snapshot footgun)**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: 0 warnings, 0 errors. This regenerates the model snapshot the
migration tool reads next.

- [ ] **Step 3: Restore the pinned EF tool (first time only)**

Run: `dotnet tool restore`

- [ ] **Step 4: Scaffold the migration**

Run:
```bash
dotnet ef migrations add AddPatientCodeSequence \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context PatientDbContext \
  --output-dir Patient
```
Expected: a new file
`src/Host/FSH.Starter.Migrations.PostgreSQL/Patient/{timestamp}_AddPatientCodeSequence.cs`
whose `Up` method contains a single
`migrationBuilder.CreateSequence<long>(name: "PatientCodeSequence", schema: "patient", startValue: 100000L, incrementBy: 1L);`
call (and the matching `DropSequence` in `Down`). `PatientDbContextModelSnapshot.cs`
is updated to include the sequence. No other table/column changes should
appear — if any do, STOP and report (that would mean an unrelated model
drift got captured).

- [ ] **Step 5: Review the generated SQL**

Run:
```bash
dotnet ef migrations script --idempotent \
  --project src/Host/FSH.Starter.Migrations.PostgreSQL \
  --startup-project src/Host/FSH.Starter.Api \
  --context PatientDbContext
```
Expected: the tail of the script contains
`CREATE SEQUENCE "patient"."PatientCodeSequence" START WITH 100000 INCREMENT BY 1 ...`
(exact clause order may vary by Npgsql provider version) and nothing else
destructive. No table drops, no column drops.

- [ ] **Step 6: Commit**

```bash
git add src/Modules/Patient/Modules.Patient/Data/PatientDbContext.cs src/Host/FSH.Starter.Migrations.PostgreSQL/Patient
git commit -m "feat(patient): add PatientCodeSequence for auto-generated codes

Global Postgres sequence, starts at 100000, patient schema. No data
migration; migrated legacy codes (P-{pId}) are untouched.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: `IPatientCodeGenerator` seam + `SequentialPatientCodeGenerator`

**Files:**
- Create: `src/Modules/Patient/Modules.Patient/Infrastructure/IPatientCodeGenerator.cs`
- Create: `src/Modules/Patient/Modules.Patient/Infrastructure/SequentialPatientCodeGenerator.cs`
- Modify: `src/Modules/Patient/Modules.Patient/PatientModule.cs`

**Interfaces:**
- Produces: `IPatientCodeGenerator.GenerateNextCodeAsync(CancellationToken): Task<string>` — returns a fully-formatted code like `"P-100000"`. This is what Task 3's handler consumes (and what Task 3's unit test mocks with `Substitute.For<IPatientCodeGenerator>()`).

This class does raw ADO SQL against the sequence created in Task 1 — it
cannot be meaningfully unit-tested without a real Postgres connection (the
EF Core InMemory provider used in Task 3's handler test has no real ADO
connection to run `nextval()` against). Its correctness is verified via the
manual smoke-test step in Task 7, not a Patient.Tests unit test. This is why
the handler depends on the *interface*, not this class directly — so the
handler itself stays fully unit-testable.

- [ ] **Step 1: Create the interface**

Create `src/Modules/Patient/Modules.Patient/Infrastructure/IPatientCodeGenerator.cs`:

```csharp
namespace FSH.Modules.Patient.Infrastructure;

public interface IPatientCodeGenerator
{
    Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Create the sequence-backed implementation**

Create `src/Modules/Patient/Modules.Patient/Infrastructure/SequentialPatientCodeGenerator.cs`:

```csharp
using System.Globalization;
using FSH.Modules.Patient.Data;

namespace FSH.Modules.Patient.Infrastructure;

public sealed class SequentialPatientCodeGenerator(PatientDbContext dbContext) : IPatientCodeGenerator
{
    public async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""SELECT nextval('"{PatientDbContext.Schema}"."PatientCodeSequence"')""";
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            long next = Convert.ToInt64(result, CultureInfo.InvariantCulture);
            return $"P-{next}";
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}
```

- [ ] **Step 3: Register in DI**

In `PatientModule.cs`, immediately after the existing
`builder.Services.AddScoped<IPhiEncryptor, PhiEncryptor>();` line, add:

```csharp
        builder.Services.AddScoped<IPatientCodeGenerator, SequentialPatientCodeGenerator>();
```

- [ ] **Step 4: Build**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: 0 warnings, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/Modules/Patient/Modules.Patient/Infrastructure/IPatientCodeGenerator.cs src/Modules/Patient/Modules.Patient/Infrastructure/SequentialPatientCodeGenerator.cs src/Modules/Patient/Modules.Patient/PatientModule.cs
git commit -m "feat(patient): add IPatientCodeGenerator + sequence-backed impl

Seam lets CreatePatientCommandHandler be unit-tested without a real
Postgres connection; the real generator calls nextval() via raw ADO.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Make `CreatePatientCommand.PatientCode` optional + wire the generator

**Files:**
- Modify: `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/CreatePatientCommand.cs`
- Modify: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/CreatePatient/CreatePatientCommandValidator.cs`
- Modify: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/CreatePatient/CreatePatientCommandHandler.cs`
- Modify: `src/Tests/Patient.Tests/Validators/CreatePatientCommandValidatorTests.cs`
- Modify: `src/Tests/Patient.Tests/Patient.Tests.csproj` (add EF Core InMemory, test-only)
- Create: `src/Tests/Patient.Tests/Features/CreatePatientCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IPatientCodeGenerator.GenerateNextCodeAsync(CancellationToken): Task<string>` (Task 2).
- Produces: `CreatePatientCommand` with `string? PatientCode = null` (trailing optional — the MSSQL mapper uses named args, unaffected).

- [ ] **Step 1: Make `PatientCode` optional on the command**

In `CreatePatientCommand.cs`, change the first parameter from
`string PatientCode,` to `string? PatientCode = null,`. Because C# requires
optional parameters to come after required ones in a positional record, and
`PatientCode` is currently first, move it to become the **last** parameter
instead (all other parameters shift up by one position). The full record
becomes (only the parameter order/optionality changes — every parameter name
and type stays identical):

```csharp
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record CreatePatientCommand(
    bool IsActive,
    // Demographics
    string FirstName,
    string LastName,
    string? MiddleInitial,
    DateTime DateOfBirth,
    string Gender,
    string? MaritalStatus,
    bool IsMinor,
    int? RaceId,
    int? EthnicityId,
    int? LanguageId,
    int? SmokingStatusId,
    DateTime? SmokingStartDate,
    DateTime? SmokingEndDate,
    string? MedicalAlertNotes,
    // Contact
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? ZipCode,
    string? Phone,
    string? PhoneExtension,
    string? CellPhone,
    string? Email,
    int? PreferredContactMethodId,
    // PHI (plaintext — encrypted by handler before persistence)
    string? Ssn,
    string? GuardianSsn,
    // Employment (optional)
    string? Occupation,
    string? EmployerName,
    string? EmployerAddress1,
    string? EmployerAddress2,
    string? EmployerCity,
    string? EmployerState,
    string? EmployerZipCode,
    string? EmployerPhone,
    string? EmployerPhoneExtension,
    // Guardian (required when IsMinor = true)
    string? GuardianFirstName,
    string? GuardianLastName,
    string? GuardianMiddleInitial,
    DateTime? GuardianDateOfBirth,
    string? GuardianGender,
    string? GuardianMaritalStatus,
    string? GuardianAddress1,
    string? GuardianAddress2,
    string? GuardianCity,
    string? GuardianState,
    string? GuardianZipCode,
    string? GuardianPhone,
    string? GuardianCellPhone,
    string? GuardianEmployerName,
    string? GuardianEmployerAddress1,
    string? GuardianEmployerAddress2,
    string? GuardianEmployerCity,
    string? GuardianEmployerState,
    string? GuardianEmployerZipCode,
    // Next of Kin (optional)
    string? NextOfKinFirstName,
    string? NextOfKinLastName,
    string? NextOfKinPhone,
    string? NextOfKinRelation,
    string? NextOfKinRelationRoleCode,
    // Insurance (optional)
    string? InsuredFullName,
    DateTime? InsuredDateOfBirth,
    string? InsuredEmployerName,
    int? ReferralTypeId,
    // Flags
    bool HasNoKnownProblems = false,
    bool HasNoKnownMedications = false,
    bool HasNoKnownAllergies = false,
    bool ReceivesEmailReminders = false,
    DateTime? LastVisitDate = null,
    DateTime? NextVisitDate = null,
    // Legacy linkage — source pUniqueID when migrated from BackChart/BronstonChiro; null otherwise.
    long? LegacyUniqueId = null,
    // Patient code. Null/blank on the dashboard's create flow -> the handler
    // auto-generates via IPatientCodeGenerator. The MSSQL migration importer
    // always supplies its own "P-{legacyPId}" value here, which is used as-is.
    string? PatientCode = null) : ICommand<Guid>;
```

> Every existing caller (`CreatePatientCommandHandler`, `MssqlPatientMapper.cs`,
> `CreatePatientCommandValidatorTests.cs`, and the frontend's JSON body — which
> binds by property name, not position) uses **named arguments** or property
> names, so this reordering does not break them. Grep for
> `new CreatePatientCommand(` and `CreatePatientCommand(` to confirm every call
> site uses named args before proceeding; if any use positional args, convert
> them to named args as part of this step.

- [ ] **Step 2: Relax the validator**

In `CreatePatientCommandValidator.cs`, replace:
```csharp
        RuleFor(x => x.PatientCode).NotEmpty().MaximumLength(50);
```
with:
```csharp
        RuleFor(x => x.PatientCode).MaximumLength(50);
```
(`MaximumLength` already passes for `null` — FluentValidation only applies
length rules to non-null values by default. Removing `NotEmpty()` is what
makes null/blank valid input.)

- [ ] **Step 3: Update the existing validator test**

In `src/Tests/Patient.Tests/Validators/CreatePatientCommandValidatorTests.cs`,
replace the two blank-code theory tests:
```csharp
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_PatientCodeIsBlank(string code)
    {
        _sut.TestValidate(Valid() with { PatientCode = code })
            .ShouldHaveValidationErrorFor(x => x.PatientCode);
    }
```
with:
```csharp
    [Fact]
    public void Validate_Should_Pass_When_PatientCodeIsNull()
    {
        _sut.TestValidate(Valid() with { PatientCode = null })
            .ShouldNotHaveValidationErrorFor(x => x.PatientCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Pass_When_PatientCodeIsBlank(string code)
    {
        _sut.TestValidate(Valid() with { PatientCode = code })
            .ShouldNotHaveValidationErrorFor(x => x.PatientCode);
    }
```
Keep `Validate_Should_Fail_When_PatientCodeExceedsMaxLength` unchanged (still
a real requirement when a code *is* provided).

- [ ] **Step 4: Run the validator tests to confirm they pass**

Run: `dotnet test src/Tests/Patient.Tests/Patient.Tests.csproj --filter "FullyQualifiedName~CreatePatientCommandValidatorTests"`
Expected: PASS (all tests, including the two new ones).

- [ ] **Step 5: Wire the generator into the handler**

In `CreatePatientCommandHandler.cs`, change the class declaration and the
start of `Handle` from:
```csharp
public sealed class CreatePatientCommandHandler(PatientDbContext dbContext, IPhiEncryptor phi)
    : ICommandHandler<CreatePatientCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool codeTaken = await dbContext.Patients
            .AnyAsync(p => p.PatientCode == command.PatientCode, cancellationToken)
            .ConfigureAwait(false);
        if (codeTaken)
        {
            throw new CustomException(
                $"A patient with code '{command.PatientCode}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
```
to:
```csharp
public sealed class CreatePatientCommandHandler(
    PatientDbContext dbContext, IPhiEncryptor phi, IPatientCodeGenerator codeGenerator)
    : ICommandHandler<CreatePatientCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string patientCode = string.IsNullOrWhiteSpace(command.PatientCode)
            ? await codeGenerator.GenerateNextCodeAsync(cancellationToken).ConfigureAwait(false)
            : command.PatientCode;

        bool codeTaken = await dbContext.Patients
            .AnyAsync(p => p.PatientCode == patientCode, cancellationToken)
            .ConfigureAwait(false);
        if (codeTaken)
        {
            throw new CustomException(
                $"A patient with code '{patientCode}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }
```
Then, further down, change the one remaining use of `command.PatientCode`
(inside `Domain.Patient.Create(...)`) to use the local `patientCode` variable
instead:
```csharp
        var patient = Domain.Patient.Create(
            patientCode, command.IsActive,
            demographics, contact, phiValue,
            employment, guardian, nextOfKin, insurance,
            command.HasNoKnownProblems, command.HasNoKnownMedications, command.HasNoKnownAllergies,
            command.ReceivesEmailReminders, command.LastVisitDate, command.NextVisitDate,
            command.LegacyUniqueId);
```
No new `using` is needed — `using FSH.Modules.Patient.Infrastructure;` is
already present in this file (it's where `IPhiEncryptor` lives, and
`IPatientCodeGenerator` from Task 2 lives in the same namespace).

- [ ] **Step 6: Build**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: 0 warnings, 0 errors.

- [ ] **Step 7: Add EF Core InMemory as a test-only dependency**

The handler test below needs a real (in-memory) `PatientDbContext` for the
`codeTaken`/`Add`/`SaveChanges` path — the codebase already uses this exact
technique in `Webhooks.Tests`. `Microsoft.EntityFrameworkCore.InMemory` is
already centrally pinned at version `10.0.8` in `src/Directory.Packages.props`
— no version-pin edit needed. Add the package reference to
`src/Tests/Patient.Tests/Patient.Tests.csproj`, in the same
`<ItemGroup>` as the other `PackageReference` entries:
```xml
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
```
Run `dotnet restore` after editing.

- [ ] **Step 8: Write the failing handler test**

Create `src/Tests/Patient.Tests/Features/CreatePatientCommandHandlerTests.cs`.
This constructs an InMemory-backed `PatientDbContext` (mirroring the stub
accessor/environment pattern from `PatientDbContextConstructorTests.cs`,
swapping the Npgsql provider for InMemory) and a mocked `IPatientCodeGenerator`:

```csharp
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Features.v1.Patients.CreatePatient;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class CreatePatientCommandHandlerTests
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

    private static CreatePatientCommand ValidCommand(string? patientCode) => new(
        IsActive: true,
        FirstName: "John",
        LastName: "Doe",
        MiddleInitial: null,
        DateOfBirth: new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Gender: "M",
        MaritalStatus: null,
        IsMinor: false,
        RaceId: null, EthnicityId: null, LanguageId: null,
        SmokingStatusId: null, SmokingStartDate: null, SmokingEndDate: null,
        MedicalAlertNotes: null,
        Address1: null, Address2: null, City: null, State: null, ZipCode: null,
        Phone: null, PhoneExtension: null, CellPhone: null,
        Email: null, PreferredContactMethodId: null,
        Ssn: null, GuardianSsn: null,
        Occupation: null, EmployerName: null,
        EmployerAddress1: null, EmployerAddress2: null,
        EmployerCity: null, EmployerState: null, EmployerZipCode: null,
        EmployerPhone: null, EmployerPhoneExtension: null,
        GuardianFirstName: null, GuardianLastName: null, GuardianMiddleInitial: null,
        GuardianDateOfBirth: null, GuardianGender: null, GuardianMaritalStatus: null,
        GuardianAddress1: null, GuardianAddress2: null,
        GuardianCity: null, GuardianState: null, GuardianZipCode: null,
        GuardianPhone: null, GuardianCellPhone: null,
        GuardianEmployerName: null, GuardianEmployerAddress1: null, GuardianEmployerAddress2: null,
        GuardianEmployerCity: null, GuardianEmployerState: null, GuardianEmployerZipCode: null,
        NextOfKinFirstName: null, NextOfKinLastName: null, NextOfKinPhone: null,
        NextOfKinRelation: null, NextOfKinRelationRoleCode: null,
        InsuredFullName: null, InsuredDateOfBirth: null, InsuredEmployerName: null, ReferralTypeId: null,
        HasNoKnownProblems: false, HasNoKnownMedications: false, HasNoKnownAllergies: false,
        ReceivesEmailReminders: false,
        LastVisitDate: null, NextVisitDate: null,
        LegacyUniqueId: null,
        PatientCode: patientCode);

    [Fact]
    public async Task Handle_Should_Generate_Code_When_PatientCodeIsNull()
    {
        using var dbContext = CreateContext(Guid.NewGuid().ToString());
        var codeGenerator = Substitute.For<IPatientCodeGenerator>();
        codeGenerator.GenerateNextCodeAsync(Arg.Any<CancellationToken>()).Returns("P-100000");
        var sut = new CreatePatientCommandHandler(dbContext, Substitute.For<IPhiEncryptor>(), codeGenerator);

        Guid id = await sut.Handle(ValidCommand(patientCode: null), CancellationToken.None);

        var saved = await dbContext.Patients.FindAsync(id);
        saved.ShouldNotBeNull();
        saved!.PatientCode.ShouldBe("P-100000");
        await codeGenerator.Received(1).GenerateNextCodeAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_Should_Generate_Code_When_PatientCodeIsBlank(string blank)
    {
        using var dbContext = CreateContext(Guid.NewGuid().ToString());
        var codeGenerator = Substitute.For<IPatientCodeGenerator>();
        codeGenerator.GenerateNextCodeAsync(Arg.Any<CancellationToken>()).Returns("P-100001");
        var sut = new CreatePatientCommandHandler(dbContext, Substitute.For<IPhiEncryptor>(), codeGenerator);

        Guid id = await sut.Handle(ValidCommand(patientCode: blank), CancellationToken.None);

        var saved = await dbContext.Patients.FindAsync(id);
        saved!.PatientCode.ShouldBe("P-100001");
    }

    [Fact]
    public async Task Handle_Should_UseProvidedCode_When_PatientCodeIsGiven()
    {
        using var dbContext = CreateContext(Guid.NewGuid().ToString());
        var codeGenerator = Substitute.For<IPatientCodeGenerator>();
        var sut = new CreatePatientCommandHandler(dbContext, Substitute.For<IPhiEncryptor>(), codeGenerator);

        Guid id = await sut.Handle(ValidCommand(patientCode: "P-4821"), CancellationToken.None);

        var saved = await dbContext.Patients.FindAsync(id);
        saved!.PatientCode.ShouldBe("P-4821");
        await codeGenerator.DidNotReceive().GenerateNextCodeAsync(Arg.Any<CancellationToken>());
    }
}
```

> `IPhiEncryptor.Encrypt`/`Decrypt`/`HashForSearch` on the substitute return
> `null` by default (NSubstitute default), which is fine here since the test
> patient has no SSN/PHI set.

- [ ] **Step 9: Run the test to verify it fails first (RED)**

Run: `dotnet test src/Tests/Patient.Tests/Patient.Tests.csproj --filter "FullyQualifiedName~CreatePatientCommandHandlerTests"`
Expected: FAIL to compile/run before Step 5's handler change is in place —
if you're doing Steps 5 and 8 in order as written above, the handler change
already landed first; to see genuine RED, temporarily verify by reverting
Step 5's `codeGenerator` wiring locally, confirming the test fails, then
reapplying Step 5. (If Step 5 is already committed, this red/green
distinction is implicit in "the test could not have passed against the old
3-arg-less constructor" — proceed to Step 10.)

- [ ] **Step 10: Run the test to verify it passes (GREEN)**

Run: `dotnet test src/Tests/Patient.Tests/Patient.Tests.csproj --filter "FullyQualifiedName~CreatePatientCommandHandlerTests"`
Expected: PASS — all 4 test cases (2 null/blank-generates via `[Theory]`, 1
null-generates, 1 provided-code-passthrough).

- [ ] **Step 11: Run the full Patient.Tests suite**

Run: `dotnet test src/Tests/Patient.Tests/Patient.Tests.csproj`
Expected: PASS — no regressions in existing validator/domain/encryptor tests.

- [ ] **Step 12: Commit**

```bash
git add src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/CreatePatientCommand.cs src/Modules/Patient/Modules.Patient/Features/v1/Patients/CreatePatient src/Tests/Patient.Tests/Validators/CreatePatientCommandValidatorTests.cs src/Tests/Patient.Tests/Features/CreatePatientCommandHandlerTests.cs src/Tests/Patient.Tests/Patient.Tests.csproj
git commit -m "feat(patient): auto-generate PatientCode when not supplied

CreatePatientCommand.PatientCode is now optional; the handler generates via
IPatientCodeGenerator when null/blank, and passes through an explicit code
(the MSSQL migration path) unchanged. Handler unit-tested with EF InMemory
+ a mocked generator — no real Postgres connection needed.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Next-code preview endpoint

**Files:**
- Create: `src/Modules/Patient/Modules.Patient.Contracts/Dtos/NextPatientCodePreviewDto.cs`
- Create: `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/NextPatientCodePreviewQuery.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/NextPatientCodePreview/NextPatientCodePreviewQueryHandler.cs`
- Create: `src/Modules/Patient/Modules.Patient/Features/v1/Patients/NextPatientCodePreview/NextPatientCodePreviewEndpoint.cs`
- Modify: `src/Modules/Patient/Modules.Patient/PatientModule.cs`

**Interfaces:**
- Produces: `GET /api/v1/patient/patients/next-code-preview` → `200 OK` with body `{ "preview": "P-100007" }`. Consumed by Task 5's frontend `getNextPatientCodePreview()`.

This is a read-only peek (does NOT call `nextval()` — never consumes the
sequence), per the spec's "preview only, assign at Save" decision.

- [ ] **Step 1: Create the response DTO**

Create `src/Modules/Patient/Modules.Patient.Contracts/Dtos/NextPatientCodePreviewDto.cs`:

```csharp
namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record NextPatientCodePreviewDto(string Preview);
```

- [ ] **Step 2: Create the query**

Create `src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/NextPatientCodePreviewQuery.cs`:

```csharp
using FSH.Modules.Patient.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.Patients;

public sealed record NextPatientCodePreviewQuery : IQuery<NextPatientCodePreviewDto>;
```

- [ ] **Step 3: Create the handler**

Create `src/Modules/Patient/Modules.Patient/Features/v1/Patients/NextPatientCodePreview/NextPatientCodePreviewQueryHandler.cs`:

```csharp
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;

namespace FSH.Modules.Patient.Features.v1.Patients.NextPatientCodePreview;

public sealed class NextPatientCodePreviewQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<NextPatientCodePreviewQuery, NextPatientCodePreviewDto>
{
    public async ValueTask<NextPatientCodePreviewDto> Handle(
        NextPatientCodePreviewQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        await dbContext.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            using var command = connection.CreateCommand();
            command.CommandText = $"""SELECT last_value, is_called FROM "{PatientDbContext.Schema}"."PatientCodeSequence" """;
            using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            long lastValue = reader.GetInt64(0);
            bool isCalled = reader.GetBoolean(1);
            long next = isCalled ? lastValue + 1 : lastValue;
            return new NextPatientCodePreviewDto($"P-{next}");
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}
```

> No validator needed — this query has no parameters, so there is nothing to
> validate (it is not a paginated query, so golden rule 8 does not apply).

- [ ] **Step 4: Create the endpoint**

Create `src/Modules/Patient/Modules.Patient/Features/v1/Patients/NextPatientCodePreview/NextPatientCodePreviewEndpoint.cs`:

```csharp
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.NextPatientCodePreview;

public static class NextPatientCodePreviewEndpoint
{
    internal static RouteHandlerBuilder MapNextPatientCodePreviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/patients/next-code-preview",
                async (IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new NextPatientCodePreviewQuery(), ct)))
            .WithName("NextPatientCodePreview")
            .WithSummary("Peek the next auto-generated patient code (does not consume it)")
            .RequirePermission(PatientPermissions.Patients.Create);
    }
}
```

> Route ordering note: `/patients/next-code-preview` must be registered
> before/alongside `/patients/{id}`-shaped routes without conflict — ASP.NET
> Core's endpoint routing resolves literal segments (`next-code-preview`)
> before parameterized ones, so no explicit ordering is required, but verify
> in Step 6 that a manual request to this route does not get swallowed by
> `GetPatientById`'s `/patients/{id}` route.

- [ ] **Step 5: Wire the endpoint into the module**

In `PatientModule.cs`, immediately after the existing
`group.MapSearchPatientsEndpoint();` line, add:
```csharp
        group.MapNextPatientCodePreviewEndpoint();
```

- [ ] **Step 6: Build**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: 0 warnings, 0 errors.

- [ ] **Step 7: Run Architecture.Tests**

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj`
Expected: PASS — 51/51 (this is a non-paginated query, so no validator is
required by `HandlerValidatorPairingTests`; the endpoint name
`NextPatientCodePreviewEndpoint` starts with a recognized pattern — verify
against `EndpointConventionTests`'s allow-list; if it fails because the
convention test doesn't recognize "peek"-style GET names, rename the class
to start with a recognized verb, e.g. `GetNextPatientCodePreviewEndpoint`,
and update the `MapEndpoints`/DI wiring accordingly before re-running).

- [ ] **Step 8: Commit**

```bash
git add src/Modules/Patient/Modules.Patient.Contracts/Dtos/NextPatientCodePreviewDto.cs src/Modules/Patient/Modules.Patient.Contracts/v1/Patients/NextPatientCodePreviewQuery.cs src/Modules/Patient/Modules.Patient/Features/v1/Patients/NextPatientCodePreview src/Modules/Patient/Modules.Patient/PatientModule.cs
git commit -m "feat(patient): add next-code preview endpoint

GET /api/v1/patient/patients/next-code-preview peeks the sequence without
consuming it (SELECT last_value, is_called — the standard Postgres idiom).
Best-effort preview only; the authoritative code is assigned at Create.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Frontend API — preview call + optional `patientCode` on create

**Files:**
- Modify: `clients/dashboard/src/api/patients.ts`

**Interfaces:**
- Produces: `getNextPatientCodePreview(): Promise<string>`; `CreatePatientInput` no longer requires `patientCode`.

- [ ] **Step 1: Narrow `CreatePatientInput`'s `patientCode` to optional**

In `patients.ts`, `PatientFields` stays exactly as-is (it's also used by
`UpdatePatientInput`, where `patientCode` must remain required — the update
flow always supplies the current code via `patient-mappers.ts`'s merge).
Replace:
```typescript
export type CreatePatientInput = PatientFields;
export type UpdatePatientInput = PatientFields & { patientId: string };
```
with:
```typescript
export type CreatePatientInput = Omit<PatientFields, "patientCode"> & { patientCode?: string };
export type UpdatePatientInput = PatientFields & { patientId: string };
```

- [ ] **Step 2: Add the preview call**

Immediately after the existing `createPatient` function, add:
```typescript
export async function getNextPatientCodePreview(): Promise<string> {
  const result = await apiFetch<{ preview: string }>(
    "/api/v1/patient/patients/next-code-preview",
  );
  return result.preview;
}
```

- [ ] **Step 3: Typecheck**

Run: `cd clients/dashboard && npm run build`
Expected: `tsc -b` passes, vite build completes. If `CreatePatientInput`'s
narrower type surfaces a type error anywhere else it's used (search for
other `CreatePatientInput`/`createPatient(` call sites besides the create
dialog), fix those call sites to match — there should be none besides
`create-patient-dialog.tsx` (Task 6).

- [ ] **Step 4: Commit**

```bash
git add clients/dashboard/src/api/patients.ts
git commit -m "feat(dashboard): add next-code preview call; patientCode optional on create

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Create-patient dialog — read-only preview field

**Files:**
- Modify: `clients/dashboard/src/pages/patients/create-patient-dialog.tsx`
- Test: `clients/dashboard/tests/patients/patients.spec.ts` (extend the existing "opens the Register a patient dialog" coverage)

**Interfaces:**
- Consumes: `getNextPatientCodePreview()` (Task 5).

- [ ] **Step 1: Write the failing E2E assertion**

In `clients/dashboard/tests/patients/patients.spec.ts`, find the existing test
`"opens the Register a patient dialog with its key fields"` (inside
`test.describe("patients — list", ...)`). Add a mock for the preview
endpoint in that test (before `page.goto`) and a new assertion after the
existing `dialog.getByLabel("Patient code")` check:

```typescript
  test("opens the Register a patient dialog with its key fields", async ({ page }) => {
    await mockJsonResponse(page, "**/api/v1/patient/patients**", paged([PATIENT_LIST_ALICE]));
    await mockJsonResponse(page, "**/api/v1/patient/patients/next-code-preview", { preview: "P-100007" });

    await page.goto("/patients");
    await page.getByRole("button", { name: /new patient/i }).first().click();

    const dialog = page.getByRole("dialog");
    await expect(dialog).toBeVisible();
    await expect(dialog.getByRole("heading", { name: /register a patient/i })).toBeVisible();
    const codeField = dialog.getByLabel("Patient code");
    await expect(codeField).toHaveValue("P-100007");
    await expect(codeField).toBeDisabled();
    await expect(dialog.getByLabel("First name")).toBeVisible();
    await expect(dialog.getByLabel("Last name")).toBeVisible();
    await expect(dialog.getByLabel("Gender")).toBeVisible();
    await expect(dialog.getByLabel("Marital status")).toBeVisible();
  });
```

> If this test currently lives in a describe block that gets removed by a
> different in-flight change, apply this update wherever the "opens the
> Register a patient dialog" test currently lives — search
> `clients/dashboard/tests/` for that exact test name if it's not in
> `patients.spec.ts`.

- [ ] **Step 2: Run the test to verify it fails**

Run: `cd clients/dashboard && npx playwright test -g "opens the Register a patient dialog"`
Expected: FAIL — the current dialog's Patient code field is an editable,
empty, required text input, not a disabled field pre-filled with "P-100007".

- [ ] **Step 3: Rewrite the dialog's Patient code field**

In `create-patient-dialog.tsx`:

1. Add the import:
```typescript
import { useQuery } from "@tanstack/react-query";
```
merge it into the existing `import { useMutation, useQueryClient } from "@tanstack/react-query";`
line so it reads:
```typescript
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
```

2. Add `getNextPatientCodePreview` to the existing patients-api import:
```typescript
import {
  createPatient,
  getNextPatientCodePreview,
  type CreatePatientInput,
} from "@/api/patients";
```

3. Remove the `patientCode` state and its reset line:
```typescript
  const [patientCode, setPatientCode] = useState("");
```
and remove `setPatientCode("");` from the `useEffect` reset block.

4. Add the preview query (place it after the existing `useEffect` reset
   block, before the `mutation` declaration):
```typescript
  const previewQuery = useQuery({
    queryKey: ["patients", "next-code-preview"],
    queryFn: getNextPatientCodePreview,
    enabled: open,
    staleTime: 0,
  });
```

5. Replace the Patient code field:
```tsx
            <Field id="pat-code" label="Patient code" required>
              <Input
                id="pat-code"
                value={patientCode}
                onChange={(e) => setPatientCode(e.target.value)}
                placeholder="P-10293"
                autoFocus
                required
              />
            </Field>
```
with:
```tsx
            <Field id="pat-code" label="Patient code">
              <Input
                id="pat-code"
                value={previewQuery.data ?? (previewQuery.isLoading ? "Generating…" : "")}
                disabled
                readOnly
              />
            </Field>
```

6. Move `autoFocus` to the First name field (the code field is no longer the
   natural first focus target):
```tsx
              <Field id="pat-first" label="First name" required>
                <Input
                  id="pat-first"
                  value={firstName}
                  onChange={(e) => setFirstName(e.target.value)}
                  placeholder="Ada"
                  autoFocus
                  required
                />
              </Field>
```

7. In `onSubmit`, stop sending a user-typed code — override it to `undefined`
   so it's omitted from the outgoing JSON:
```typescript
  const onSubmit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    if (!gender) return;
    mutation.mutate({
      ...emptyPatientFields(),
      patientCode: undefined,
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      middleInitial: middleInitial.trim() || null,
      dateOfBirth,
      gender,
      maritalStatus,
    });
  };
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `cd clients/dashboard && npx playwright test -g "opens the Register a patient dialog"`
Expected: PASS.

- [ ] **Step 5: Typecheck + lint**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean (no new problems versus whatever the pre-existing baseline
is at the time this task runs — check with `git stash`/`git stash pop`
around a `npm run lint` run if unsure which errors are pre-existing).

- [ ] **Step 6: Run the full patients E2E spec**

Run: `cd clients/dashboard && npx playwright test tests/patients/patients.spec.ts --workers=1`
Expected: PASS (this suite is flaky under default parallel workers on some
machines — use `--workers=1` for a reliable result).

- [ ] **Step 7: Commit**

```bash
git add clients/dashboard/src/pages/patients/create-patient-dialog.tsx clients/dashboard/tests/patients/patients.spec.ts
git commit -m "feat(dashboard): read-only auto-generated code in create-patient dialog

Patient code field previews the next server-assigned code and is no longer
user-editable; the create payload omits patientCode so the server always
assigns the authoritative value.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Full verification + deferred live-sequence smoke test

**Files:** none (verification only)

- [ ] **Step 1: Backend build + full test suite**

Run: `dotnet build src/FSH.Starter.slnx`
Expected: 0 warnings, 0 errors.

Run: `dotnet test src/Tests/Architecture.Tests/Architecture.Tests.csproj`
Expected: PASS — 51/51.

Run: `dotnet test src/Tests/Patient.Tests/Patient.Tests.csproj`
Expected: PASS — all tests including the new
`CreatePatientCommandHandlerTests` (4 cases) and the updated validator tests.

- [ ] **Step 2: Frontend build, lint, E2E**

Run: `cd clients/dashboard && npm run build && npm run lint`
Expected: clean.

Run: `cd clients/dashboard && npx playwright test tests/patients --workers=1`
Expected: PASS.

- [ ] **Step 3: Manual smoke test against a real Postgres (deferred — requires a live DB)**

This step verifies the ONE thing that cannot be unit-tested: that
`nextval()` on a real Postgres sequence behaves as designed end-to-end. If a
running Postgres + API are available (e.g. via
`dotnet run --project src/Host/FSH.Starter.AppHost`), after applying the
migration (`dotnet run --project src/Host/FSH.Starter.DbMigrator -- apply`):

1. Call the preview endpoint twice in a row (e.g. via `/scalar` or curl) —
   confirm it returns the SAME value both times (since peeking doesn't
   consume the sequence).
2. Create two patients back-to-back via the dashboard's "New patient"
   dialog, without entering a code — confirm they get DIFFERENT codes,
   each one higher than the previewed value from step 1, in the
   `P-100000`, `P-100001`, ... shape.
3. Confirm a migrated/legacy patient (if any exist in the test data) still
   shows its `P-{legacyPId}` code, untouched.

If no live Postgres is available in this session, flag this step to the
user as a required follow-up before merging — the same pattern this
project has used for prior features that needed a live-DB verification.

---

## Self-Review

**Spec coverage:**
- Real Postgres SEQUENCE, not MAX+1/retry → Task 1, Task 2. ✓
- Format `P-<number>`, floor 100,000, no padding → Task 1 Step 1. ✓
- One global sequence (not per-tenant) → Task 1 (single sequence in the shared `patient` schema, no tenant parameter). ✓
- `CreatePatientCommand.PatientCode` optional; migration importer untouched → Task 3 Step 1 (only the command signature changes; `MssqlPatientMapper.cs` already uses named args and keeps supplying its own code). ✓
- Handler auto-generates when null/blank, passes through when provided → Task 3 Step 5, tested in Task 3 Steps 8–10. ✓
- Preview endpoint peeks without consuming, gated by `Patients.Create` → Task 4. ✓
- Dialog: read-only, pre-filled, omits `patientCode` on submit → Task 6. ✓
- Testing: validator, handler (mocked generator + InMemory DbContext), Playwright → Tasks 3, 6. ✓
- Deferred: real-Postgres `nextval()` behavior verification → Task 7 Step 3 (explicitly flagged, not silently skipped). ✓

**Placeholder scan:** No TBD/TODO; every code step shows full code (the one
exception — Task 3 Step 9's RED-verification note — explicitly explains why
a literal separate revert-and-rerun isn't mechanically prescribed, rather
than hand-waving past it). ✓

**Type consistency:** `IPatientCodeGenerator.GenerateNextCodeAsync(CancellationToken): Task<string>` is defined in Task 2 and consumed identically in Task 3's handler and mocked identically in Task 3's test. `NextPatientCodePreviewDto(string Preview)` (Task 4) matches the frontend's `{ preview: string }` shape (Task 5) — note the JSON property name serializes as camelCase `preview` per this codebase's established System.Text.Json convention (confirmed elsewhere, e.g. `PatientPhiDto`'s `phi` casing note in `api/patients.ts`). `CreatePatientInput` (Task 5) matches what `create-patient-dialog.tsx` submits (Task 6 Step 3.7 — `patientCode: undefined`). ✓
