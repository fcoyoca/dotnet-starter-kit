using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientIncidents.RestorePatientIncident;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class RestorePatientIncidentHandlerTests
{
    private static async Task<PatientIncident> SeedDeletedIncident(PatientDbContext db)
    {
        PatientIncident incident = PatientIncident.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            isTransfer: false,
            isAccident: false,
            accidentType: null,
            accidentState: null,
            comments: null);
        incident.Delete("tester");
        db.PatientIncidents.Add(incident);
        await db.SaveChangesAsync();
        return incident;
    }

    [Fact]
    public async Task Restore_Should_Clear_Deletion_State()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedDeletedIncident(db);
        var sut = new RestorePatientIncidentCommandHandler(db);

        await sut.Handle(new RestorePatientIncidentCommand(incident.Id), CancellationToken.None);

        PatientIncident saved = await db.PatientIncidents
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == incident.Id);
        saved.IsDeleted.ShouldBeFalse();
        saved.DeletedOnUtc.ShouldBeNull();
        saved.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task Restore_Should_Throw_NotFound_For_Unknown_Incident()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        var sut = new RestorePatientIncidentCommandHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new RestorePatientIncidentCommand(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Restore_Should_Throw_NotFound_When_Incident_Is_Not_Deleted()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedDeletedIncident(db);
        incident.Restore();
        await db.SaveChangesAsync();
        var sut = new RestorePatientIncidentCommandHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new RestorePatientIncidentCommand(incident.Id), CancellationToken.None).AsTask());
    }
}
