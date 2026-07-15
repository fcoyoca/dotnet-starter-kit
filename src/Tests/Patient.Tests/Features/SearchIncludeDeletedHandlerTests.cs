using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientIncidents.SearchPatientIncidents;
using FSH.Modules.Patient.Features.v1.PatientReports.SearchPatientReports;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

/// <summary>
/// PatientIncident and PatientReport are ISoftDeletable, so a global query filter hides
/// deleted rows. These lock in that <c>IncludeDeleted</c> bypasses only that filter — a
/// plain <c>Where(!IsDeleted)</c> could never surface them because the global filter re-hides them.
/// </summary>
public sealed class SearchIncludeDeletedHandlerTests
{
    private static async Task<(PatientReport active, PatientReport deleted)> SeedReports(PatientDbContext db, Guid patientId)
    {
        PatientReport active = PatientReport.Create(Guid.NewGuid(), patientId, 1, DateTime.UtcNow.Date, null, null, false);
        PatientReport deleted = PatientReport.Create(Guid.NewGuid(), patientId, 1, DateTime.UtcNow.Date, null, null, false);
        deleted.Delete("tester");
        db.PatientReports.AddRange(active, deleted);
        await db.SaveChangesAsync();
        return (active, deleted);
    }

    [Fact]
    public async Task Reports_Default_Excludes_Deleted()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        (PatientReport active, _) = await SeedReports(db, patientId);
        var sut = new SearchPatientReportsQueryHandler(db);

        var result = await sut.Handle(new SearchPatientReportsQuery(PatientId: patientId), CancellationToken.None);

        result.Items.Select(x => x.Id).ShouldBe([active.Id]);
    }

    [Fact]
    public async Task Reports_IncludeDeleted_Surfaces_Deleted_With_Flag()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        (_, PatientReport deleted) = await SeedReports(db, patientId);
        var sut = new SearchPatientReportsQueryHandler(db);

        var result = await sut.Handle(
            new SearchPatientReportsQuery(PatientId: patientId, IncludeDeleted: true), CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.Items.Single(x => x.Id == deleted.Id).IsDeleted.ShouldBeTrue();
    }

    private static async Task<(PatientIncident active, PatientIncident deleted)> SeedIncidents(PatientDbContext db, Guid patientId)
    {
        PatientIncident MakeIncident() => PatientIncident.Create(
            patientId, Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.Date, DateTime.UtcNow.Date,
            isTransfer: false, isAccident: false, accidentType: null, accidentState: null, comments: null);
        PatientIncident active = MakeIncident();
        PatientIncident deleted = MakeIncident();
        deleted.Delete("tester");
        db.PatientIncidents.AddRange(active, deleted);
        await db.SaveChangesAsync();
        return (active, deleted);
    }

    [Fact]
    public async Task Incidents_Default_Excludes_Deleted()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        (PatientIncident active, _) = await SeedIncidents(db, patientId);
        var sut = new SearchPatientIncidentsQueryHandler(db);

        var result = await sut.Handle(new SearchPatientIncidentsQuery(patientId), CancellationToken.None);

        result.Items.Select(x => x.Id).ShouldBe([active.Id]);
    }

    [Fact]
    public async Task Incidents_IncludeDeleted_Surfaces_Deleted_With_Flag()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        (_, PatientIncident deleted) = await SeedIncidents(db, patientId);
        var sut = new SearchPatientIncidentsQueryHandler(db);

        var result = await sut.Handle(
            new SearchPatientIncidentsQuery(patientId, IncludeDeleted: true), CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.Items.Single(x => x.Id == deleted.Id).IsDeleted.ShouldBeTrue();
    }
}
