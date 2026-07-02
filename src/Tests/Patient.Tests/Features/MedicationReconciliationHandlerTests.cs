using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientMedications.GetMedicationReconciledDates;
using FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class MedicationReconciliationHandlerTests
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

    [Fact]
    public async Task Mark_Then_List_Returns_History_Newest_First()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = await SeedPatientAsync(db);
        var mark = new MarkMedicationsReconciledCommandHandler(db, User());
        var list = new GetMedicationReconciledDatesQueryHandler(db);

        Guid id = await mark.Handle(new MarkMedicationsReconciledCommand(patientId), CancellationToken.None);

        var dates = await list.Handle(new GetMedicationReconciledDatesQuery(patientId), CancellationToken.None);
        dates.Count.ShouldBe(1);
        dates[0].Id.ShouldBe(id);
        dates[0].ReconciledOn.Date.ShouldBe(DateTime.UtcNow.Date);
    }

    [Fact]
    public async Task Mark_Should_ThrowNotFoundException_When_PatientDoesNotExist()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        var mark = new MarkMedicationsReconciledCommandHandler(db, User());

        await Should.ThrowAsync<NotFoundException>(async () =>
            await mark.Handle(new MarkMedicationsReconciledCommand(patientId), CancellationToken.None));
    }
}
