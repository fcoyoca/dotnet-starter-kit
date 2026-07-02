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
        defaults.Items.Single().DrugName.ShouldBe("Active drug");
        all.Items.Count.ShouldBe(2);
    }
}
