using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;
using FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;
using FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class PatientMedicationHandlerTests
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

    private static async Task<Guid> SeedPatientAsync(PatientDbContext db, bool noKnownMedications = false)
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
            hasNoKnownMedications: noKnownMedications,
            hasNoKnownAllergies: false,
            receivesEmailReminders: false,
            lastVisitDate: null, nextVisitDate: null);
        db.Patients.Add(patient);
        await db.SaveChangesAsync();
        return patient.Id;
    }

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
