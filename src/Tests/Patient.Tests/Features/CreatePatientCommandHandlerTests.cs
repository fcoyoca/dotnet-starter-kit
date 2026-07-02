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
