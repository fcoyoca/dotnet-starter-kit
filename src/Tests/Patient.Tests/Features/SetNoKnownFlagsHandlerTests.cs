using System.Net;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;
using FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownMedications;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class SetNoKnownFlagsHandlerTests
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

    private static async Task<Guid> SeedPatientAsync(PatientDbContext db, bool noKnownAllergies = false)
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
            hasNoKnownAllergies: noKnownAllergies,
            receivesEmailReminders: false,
            lastVisitDate: null, nextVisitDate: null);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient.Id;
    }

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
