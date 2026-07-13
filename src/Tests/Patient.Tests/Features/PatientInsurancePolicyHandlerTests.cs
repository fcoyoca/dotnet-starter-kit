using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.CreatePatientInsurancePolicy;
using FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.DeletePatientInsurancePolicy;
using FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.GetPatientInsurancePolicyById;
using FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.SearchPatientInsurancePolicies;
using FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.UpdatePatientInsurancePolicy;
using FSH.Modules.Patient.Infrastructure;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class PatientInsurancePolicyHandlerTests
{
    private static readonly Guid Aetna = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Cigna = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PpoType = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>
    /// The subscriber SSN round-trips through <see cref="IPhiEncryptor"/> via a value converter, so the
    /// tests need a real pass-through here. A substitute would return <c>string.Empty</c> from
    /// Encrypt/Decrypt and silently blank the value out.
    /// </summary>
    private sealed class PassThroughPhiEncryptor : IPhiEncryptor
    {
        public string? Encrypt(string? plaintext) => plaintext;
        public string? Decrypt(string? ciphertext) => ciphertext;
        public string? HashForSearch(string? value) => value;
    }

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
            accessor, options, settings, Substitute.For<IHostEnvironment>(), new PassThroughPhiEncryptor());
    }

    private static ICurrentUser User()
    {
        var user = Substitute.For<ICurrentUser>();
        user.GetUserId().Returns(Guid.NewGuid());
        user.Name.Returns("Test User");
        return user;
    }

    /// <summary>The read handlers resolve payer/plan names from Administration via IMediator.</summary>
    private static IMediator Lookups()
    {
        var mediator = Substitute.For<IMediator>();

        var companies = new PagedResponse<InsuranceCompanyDto>
        {
            Items = [Company(Aetna, "Aetna"), Company(Cigna, "Cigna")],
            PageNumber = 1,
            PageSize = 200,
            TotalCount = 2,
            TotalPages = 1
        };

        var types = new PagedResponse<InsuranceTypeDto>
        {
            Items = [new InsuranceTypeDto(PpoType, "PPO", true, null, null, DateTime.UtcNow, null)],
            PageNumber = 1,
            PageSize = 200,
            TotalCount = 1,
            TotalPages = 1
        };

        // CA2012 fires on NSubstitute's arrange syntax: `mediator.Send(...)` is a call into the
        // substitute to record the expectation, not a real ValueTask being consumed. Each arranged
        // ValueTask is freshly constructed per invocation by the lambda, so nothing is awaited twice.
#pragma warning disable CA2012
        mediator.Send(Arg.Any<ListInsuranceCompaniesQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<PagedResponse<InsuranceCompanyDto>>(companies));

        mediator.Send(Arg.Any<ListInsuranceTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<PagedResponse<InsuranceTypeDto>>(types));
#pragma warning restore CA2012

        return mediator;
    }

    private static InsuranceCompanyDto Company(Guid id, string name) =>
        new(id, name, null, null, 0, null, null, null, null, null, null, true, DateTime.UtcNow, null);

    private static async Task<Guid> SeedPatientAsync(PatientDbContext db)
    {
        var patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-1000", true,
            PatientDemographics.Create(
                "John", "Doe", null,
                new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                "M", null, isMinor: false,
                null, null, null, null, null, null, null),
            PatientContact.Create(null, null, null, null, null, null, null, null, null, null),
            PatientPhi.Create(null, null, null),
            null, null, null, null,
            hasNoKnownProblems: false,
            hasNoKnownMedications: false,
            hasNoKnownAllergies: false,
            receivesEmailReminders: false,
            lastVisitDate: null, nextVisitDate: null);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient.Id;
    }

    private static CreatePatientInsurancePolicyCommand NewCommand(
        Guid patientId,
        InsurancePriority priority = InsurancePriority.Primary,
        SubscriberRelationship relationship = SubscriberRelationship.Self,
        bool isActive = true) =>
        new(
            PatientId: patientId,
            InsuranceCompanyId: Aetna,
            InsuranceTypeId: PpoType,
            Priority: priority,
            PolicyNumber: "POL-123",
            GroupNumber: "GRP-9",
            MemberId: "MBR-7",
            CoPay: 25.00m,
            Deductible: 1500.00m,
            EffectiveDate: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
            ExpirationDate: new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Unspecified),
            SubscriberRelationship: relationship,
            SubscriberFirstName: relationship == SubscriberRelationship.Self ? "John" : "Jane",
            SubscriberLastName: "Doe",
            SubscriberDateOfBirth: new DateTime(1988, 5, 4, 0, 0, 0, DateTimeKind.Unspecified),
            SubscriberGender: relationship == SubscriberRelationship.Self ? "M" : "F",
            SubscriberSsn: "123456789",
            SubscriberEmployerName: "Acme Corp",
            SubscriberAddress1: "1 Main St",
            SubscriberAddress2: null,
            SubscriberCity: "Austin",
            SubscriberState: "TX",
            SubscriberZipCode: "78701",
            Notes: "Card on file",
            IsActive: isActive);

    [Fact]
    public async Task Create_Should_Persist_Policy()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);

        var handler = new CreatePatientInsurancePolicyCommandHandler(db, User());
        Guid id = await handler.Handle(NewCommand(patientId), CancellationToken.None);

        PatientInsurancePolicy saved = await db.PatientInsurancePolicies.SingleAsync(x => x.Id == id);
        saved.PatientId.ShouldBe(patientId);
        saved.InsuranceCompanyId.ShouldBe(Aetna);
        saved.Priority.ShouldBe(InsurancePriority.Primary);
        saved.PolicyNumber.ShouldBe("POL-123");
        saved.CoPay.ShouldBe(25.00m);
        saved.Deductible.ShouldBe(1500.00m);
        saved.IsActive.ShouldBeTrue();
        saved.CreatedByName.ShouldBe("Test User");
    }

    [Fact]
    public async Task Create_Should_Allow_Many_Policies_Per_Patient()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var handler = new CreatePatientInsurancePolicyCommandHandler(db, User());

        await handler.Handle(NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None);
        await handler.Handle(NewCommand(patientId, InsurancePriority.Secondary), CancellationToken.None);
        await handler.Handle(NewCommand(patientId, InsurancePriority.Tertiary), CancellationToken.None);

        (await db.PatientInsurancePolicies.CountAsync(x => x.PatientId == patientId)).ShouldBe(3);
    }

    [Fact]
    public async Task Create_Should_Reject_Duplicate_Active_Priority()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var handler = new CreatePatientInsurancePolicyCommandHandler(db, User());

        await handler.Handle(NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None);

        await Should.ThrowAsync<CustomException>(() =>
            handler.Handle(NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Create_Should_Allow_Duplicate_Priority_When_The_Existing_One_Is_Inactive()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var handler = new CreatePatientInsurancePolicyCommandHandler(db, User());

        await handler.Handle(
            NewCommand(patientId, InsurancePriority.Primary, isActive: false), CancellationToken.None);
        Guid id = await handler.Handle(
            NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None);

        id.ShouldNotBe(Guid.Empty);
        (await db.PatientInsurancePolicies.CountAsync(x => x.PatientId == patientId)).ShouldBe(2);
    }

    [Fact]
    public async Task Create_Should_Reject_Non_Self_Subscriber_Without_Name_And_Dob()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var handler = new CreatePatientInsurancePolicyCommandHandler(db, User());

        CreatePatientInsurancePolicyCommand command = NewCommand(
            patientId, relationship: SubscriberRelationship.Spouse) with
        {
            SubscriberFirstName = null,
            SubscriberLastName = null,
            SubscriberDateOfBirth = null
        };

        await Should.ThrowAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Create_Should_Reject_Expiration_Before_Effective()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var handler = new CreatePatientInsurancePolicyCommandHandler(db, User());

        CreatePatientInsurancePolicyCommand command = NewCommand(patientId) with
        {
            EffectiveDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Unspecified),
            ExpirationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)
        };

        await Should.ThrowAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Create_Should_Throw_When_Patient_Missing()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var handler = new CreatePatientInsurancePolicyCommandHandler(db, User());

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(NewCommand(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Update_Should_Keep_Existing_Ssn_When_Command_Ssn_Is_Blank()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);

        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());
        Guid id = await create.Handle(NewCommand(patientId), CancellationToken.None);

        var update = new UpdatePatientInsurancePolicyCommandHandler(db, User());
        await update.Handle(
            new UpdatePatientInsurancePolicyCommand(
                PolicyId: id,
                InsuranceCompanyId: Cigna,
                InsuranceTypeId: PpoType,
                Priority: InsurancePriority.Secondary,
                PolicyNumber: "POL-999",
                GroupNumber: null,
                MemberId: null,
                CoPay: null,
                Deductible: null,
                EffectiveDate: null,
                ExpirationDate: null,
                SubscriberRelationship: SubscriberRelationship.Self,
                SubscriberFirstName: "John",
                SubscriberLastName: "Doe",
                SubscriberDateOfBirth: null,
                SubscriberGender: "M",
                SubscriberSsn: null, // the edit form never receives plaintext, so it posts back blank
                SubscriberEmployerName: null,
                SubscriberAddress1: null,
                SubscriberAddress2: null,
                SubscriberCity: null,
                SubscriberState: null,
                SubscriberZipCode: null,
                Notes: null,
                IsActive: true),
            CancellationToken.None);

        PatientInsurancePolicy saved = await db.PatientInsurancePolicies.SingleAsync(x => x.Id == id);
        saved.SubscriberSsn.ShouldBe("123456789");
        saved.InsuranceCompanyId.ShouldBe(Cigna);
        saved.Priority.ShouldBe(InsurancePriority.Secondary);
        saved.PolicyNumber.ShouldBe("POL-999");
        saved.UpdatedByName.ShouldBe("Test User");
    }

    [Fact]
    public async Task Update_Should_Reject_Priority_Held_By_Another_Active_Policy()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());

        await create.Handle(NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None);
        Guid secondaryId = await create.Handle(
            NewCommand(patientId, InsurancePriority.Secondary), CancellationToken.None);

        var update = new UpdatePatientInsurancePolicyCommandHandler(db, User());

        // Promoting the secondary to primary collides with the existing active primary.
        await Should.ThrowAsync<CustomException>(() =>
            update.Handle(
                new UpdatePatientInsurancePolicyCommand(
                    secondaryId, Aetna, null, InsurancePriority.Primary,
                    null, null, null, null, null, null, null,
                    SubscriberRelationship.Self, "John", "Doe", null, "M", null, null,
                    null, null, null, null, null, null, IsActive: true),
                CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Update_Should_Allow_Same_Priority_On_Itself()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());
        Guid id = await create.Handle(NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None);

        var update = new UpdatePatientInsurancePolicyCommandHandler(db, User());
        await update.Handle(
            new UpdatePatientInsurancePolicyCommand(
                id, Aetna, null, InsurancePriority.Primary,
                "POL-123", null, null, null, null, null, null,
                SubscriberRelationship.Self, "John", "Doe", null, "M", null, null,
                null, null, null, null, null, null, IsActive: true),
            CancellationToken.None);

        PatientInsurancePolicy saved = await db.PatientInsurancePolicies.SingleAsync(x => x.Id == id);
        saved.Priority.ShouldBe(InsurancePriority.Primary);
    }

    [Fact]
    public async Task Search_Should_Order_Active_First_Then_By_Priority()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());

        await create.Handle(NewCommand(patientId, InsurancePriority.Tertiary), CancellationToken.None);
        await create.Handle(NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None);
        await create.Handle(NewCommand(patientId, InsurancePriority.Secondary), CancellationToken.None);
        await create.Handle(
            NewCommand(patientId, InsurancePriority.Quaternary, isActive: false), CancellationToken.None);

        var handler = new SearchPatientInsurancePoliciesQueryHandler(db, Lookups());
        PagedResponse<PatientInsurancePolicyDto> page = await handler.Handle(
            new SearchPatientInsurancePoliciesQuery(patientId, IncludeInactive: true), CancellationToken.None);

        page.TotalCount.ShouldBe(4);
        page.Items.Select(x => x.Priority).ShouldBe(
        [
            InsurancePriority.Primary,
            InsurancePriority.Secondary,
            InsurancePriority.Tertiary,
            InsurancePriority.Quaternary // inactive, sorts last
        ]);
    }

    [Fact]
    public async Task Search_Should_Exclude_Inactive_By_Default()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());

        await create.Handle(NewCommand(patientId, InsurancePriority.Primary), CancellationToken.None);
        await create.Handle(
            NewCommand(patientId, InsurancePriority.Secondary, isActive: false), CancellationToken.None);

        var handler = new SearchPatientInsurancePoliciesQueryHandler(db, Lookups());
        PagedResponse<PatientInsurancePolicyDto> page = await handler.Handle(
            new SearchPatientInsurancePoliciesQuery(patientId), CancellationToken.None);

        page.TotalCount.ShouldBe(1);
        page.Items.Single().Priority.ShouldBe(InsurancePriority.Primary);
    }

    [Fact]
    public async Task Search_Should_Resolve_Payer_Names_And_Mask_The_Subscriber_Ssn()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());
        await create.Handle(NewCommand(patientId), CancellationToken.None);

        var handler = new SearchPatientInsurancePoliciesQueryHandler(db, Lookups());
        PagedResponse<PatientInsurancePolicyDto> page = await handler.Handle(
            new SearchPatientInsurancePoliciesQuery(patientId), CancellationToken.None);

        PatientInsurancePolicyDto dto = page.Items.Single();
        dto.InsuranceCompanyName.ShouldBe("Aetna");
        dto.InsuranceTypeName.ShouldBe("PPO");
        dto.SubscriberSsnMasked.ShouldBe("***-**-6789");
    }

    [Fact]
    public async Task GetById_Should_Return_The_Policy()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());
        Guid id = await create.Handle(NewCommand(patientId), CancellationToken.None);

        var handler = new GetPatientInsurancePolicyByIdQueryHandler(db, Lookups());
        PatientInsurancePolicyDto dto = await handler.Handle(
            new GetPatientInsurancePolicyByIdQuery(id), CancellationToken.None);

        dto.Id.ShouldBe(id);
        dto.InsuranceCompanyName.ShouldBe("Aetna");
        dto.MemberId.ShouldBe("MBR-7");
    }

    [Fact]
    public async Task GetById_Should_Throw_When_Missing()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var handler = new GetPatientInsurancePolicyByIdQueryHandler(db, Lookups());

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new GetPatientInsurancePolicyByIdQuery(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Delete_Should_Remove_The_Policy()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var create = new CreatePatientInsurancePolicyCommandHandler(db, User());
        Guid id = await create.Handle(NewCommand(patientId), CancellationToken.None);

        var handler = new DeletePatientInsurancePolicyCommandHandler(db);
        await handler.Handle(new DeletePatientInsurancePolicyCommand(id), CancellationToken.None);

        (await db.PatientInsurancePolicies.AnyAsync(x => x.Id == id)).ShouldBeFalse();
    }

    [Fact]
    public async Task Delete_Should_Throw_When_Missing()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var handler = new DeletePatientInsurancePolicyCommandHandler(db);

        await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new DeletePatientInsurancePolicyCommand(Guid.NewGuid()), CancellationToken.None).AsTask());
    }
}
