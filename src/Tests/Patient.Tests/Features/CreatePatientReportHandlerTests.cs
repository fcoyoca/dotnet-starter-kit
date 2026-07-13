using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientReports.CreatePatientReport;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

/// <summary>
/// A new report opens pre-filled with each field's default text (legacy <c>rfDefaultText</c>), which the handler
/// stamps on at creation. Report fields live in Administration, so they arrive over Mediator via Contracts.
/// </summary>
public sealed class CreatePatientReportHandlerTests
{
    private const int ReportTypeId = 7;

    private static ReportFieldDto Field(int id, string name, string? defaultText) =>
        new(id, ReportTypeId, name, "Subjective", id, IsActive: true, defaultText);

    private static async Task<PatientIncident> SeedIncident(PatientDbContext db)
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
        db.PatientIncidents.Add(incident);
        await db.SaveChangesAsync();
        return incident;
    }

    private static IMediator MediatorReturning(params ReportFieldDto[] fields)
    {
        var mediator = Substitute.For<IMediator>();

        // CA2012 fires on NSubstitute's arrange syntax: `mediator.Send(...)` is a call into the
        // substitute to record the expectation, not a real ValueTask being consumed. The lambda
        // builds a fresh ValueTask per invocation, so nothing is awaited twice.
#pragma warning disable CA2012
        mediator
            .Send(Arg.Any<ListReportFieldsQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IReadOnlyList<ReportFieldDto>>(fields));
#pragma warning restore CA2012
        return mediator;
    }

    private static CreatePatientReportCommand Command(Guid incidentId) =>
        new(incidentId, Guid.NewGuid(), ReportTypeId, DateTime.UtcNow.Date, null, null, IsNoShow: false);

    [Fact]
    public async Task Create_Should_Prefill_Fields_With_Their_Default_Text()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedIncident(db);
        IMediator mediator = MediatorReturning(
            Field(1, "Chief Complaint", "Patient presents with "),
            Field(2, "Assessment", "See Dx codes."));
        var sut = new CreatePatientReportCommandHandler(db, mediator);

        Guid id = await sut.Handle(Command(incident.Id), CancellationToken.None);

        PatientReport saved = await db.PatientReports.Include(r => r.FieldValues)
            .SingleAsync(r => r.Id == id);
        saved.FieldValues
            .ToDictionary(v => v.ReportFieldId, v => v.Text)
            .ShouldBe(new Dictionary<int, string>
            {
                [1] = "Patient presents with ",
                [2] = "See Dx codes.",
            });
    }

    [Fact]
    public async Task Create_Should_Skip_Fields_With_No_Default_Text()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedIncident(db);
        // Null and whitespace-only both mean "no boilerplate" — neither should
        // leave an empty row behind that the editor would render as touched text.
        IMediator mediator = MediatorReturning(
            Field(1, "Chief Complaint", "Boilerplate."),
            Field(2, "Objective", null),
            Field(3, "Plan", "   "));
        var sut = new CreatePatientReportCommandHandler(db, mediator);

        Guid id = await sut.Handle(Command(incident.Id), CancellationToken.None);

        PatientReport saved = await db.PatientReports.Include(r => r.FieldValues)
            .SingleAsync(r => r.Id == id);
        saved.FieldValues.Select(v => v.ReportFieldId).ShouldBe([1]);
    }

    [Fact]
    public async Task Create_Should_Leave_Report_Empty_When_No_Field_Has_A_Default()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientIncident incident = await SeedIncident(db);
        var sut = new CreatePatientReportCommandHandler(db, MediatorReturning(Field(1, "Notes", null)));

        Guid id = await sut.Handle(Command(incident.Id), CancellationToken.None);

        PatientReport saved = await db.PatientReports.Include(r => r.FieldValues)
            .SingleAsync(r => r.Id == id);
        saved.FieldValues.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_Should_Throw_When_Incident_Is_Missing()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        var sut = new CreatePatientReportCommandHandler(db, MediatorReturning());

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(Command(Guid.NewGuid()), CancellationToken.None).AsTask());
    }
}
