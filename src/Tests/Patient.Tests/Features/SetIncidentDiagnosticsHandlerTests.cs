using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientIncidents.SetIncidentDiagnostics;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class SetIncidentDiagnosticsHandlerTests
{
    private static async Task<PatientIncident> SeedIncident(PatientDbContext db, string? comments = "original comments")
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
            comments: comments);
        db.PatientIncidents.Add(incident);
        await db.SaveChangesAsync();
        return incident;
    }

    [Fact]
    public async Task Set_Should_Replace_Existing_Set()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedIncident(db);
        Guid dx1 = Guid.NewGuid();
        Guid dx2 = Guid.NewGuid();
        Guid dx3 = Guid.NewGuid();
        var sut = new SetIncidentDiagnosticsCommandHandler(db);

        await sut.Handle(new SetIncidentDiagnosticsCommand(incident.Id, [dx1, dx2]), CancellationToken.None);
        await sut.Handle(new SetIncidentDiagnosticsCommand(incident.Id, [dx3]), CancellationToken.None);

        PatientIncident saved = await db.PatientIncidents.Include(x => x.Diagnostics)
            .SingleAsync(x => x.Id == incident.Id);
        saved.Diagnostics.Single().DiagnosticId.ShouldBe(dx3);
    }

    [Fact]
    public async Task Set_Should_Allow_Empty_List_To_Clear()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedIncident(db);
        var sut = new SetIncidentDiagnosticsCommandHandler(db);

        await sut.Handle(new SetIncidentDiagnosticsCommand(incident.Id, [Guid.NewGuid()]), CancellationToken.None);
        await sut.Handle(new SetIncidentDiagnosticsCommand(incident.Id, []), CancellationToken.None);

        PatientIncident saved = await db.PatientIncidents.Include(x => x.Diagnostics)
            .SingleAsync(x => x.Id == incident.Id);
        saved.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task Set_Should_Deduplicate_Diagnostic_Ids()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedIncident(db);
        Guid dx = Guid.NewGuid();
        var sut = new SetIncidentDiagnosticsCommandHandler(db);

        await sut.Handle(new SetIncidentDiagnosticsCommand(incident.Id, [dx, dx]), CancellationToken.None);

        PatientIncident saved = await db.PatientIncidents.Include(x => x.Diagnostics)
            .SingleAsync(x => x.Id == incident.Id);
        saved.Diagnostics.Single().DiagnosticId.ShouldBe(dx);
    }

    [Fact]
    public async Task Set_Should_Throw_NotFound_For_Unknown_Incident()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        var sut = new SetIncidentDiagnosticsCommandHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new SetIncidentDiagnosticsCommand(Guid.NewGuid(), []), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Set_Should_Not_Touch_Other_Incident_Fields()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedIncident(db, comments: "keep me untouched");
        var sut = new SetIncidentDiagnosticsCommandHandler(db);

        await sut.Handle(new SetIncidentDiagnosticsCommand(incident.Id, [Guid.NewGuid()]), CancellationToken.None);

        PatientIncident saved = await db.PatientIncidents.SingleAsync(x => x.Id == incident.Id);
        saved.Comments.ShouldBe("keep me untouched");
        saved.PatientId.ShouldBe(incident.PatientId);
        saved.IncidentTypeId.ShouldBe(incident.IncidentTypeId);
        saved.DepartmentId.ShouldBe(incident.DepartmentId);
        saved.IsClosed.ShouldBeFalse();
    }
}
