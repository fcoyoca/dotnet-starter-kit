using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientReports.ExportPatientReportsPdf;
using FSH.Modules.Patient.Services;
using Mediator;
using NSubstitute;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

/// <summary>
/// The export renders the legacy BackChart report: the page orientation comes from the report's
/// clinic, and the header needs incident context (DOIV / DOL / DX) that lives outside the report row.
/// </summary>
public sealed class ExportPatientReportsPdfHandlerTests
{
    private const int ReportTypeId = 7;

    private static readonly Guid ClinicId = Guid.NewGuid();
    private static readonly Guid DiagnosticId = Guid.NewGuid();

    private static IMediator Mediator(PrintOrientation orientation)
    {
        var mediator = Substitute.For<IMediator>();

        // CA2012: NSubstitute's arrange syntax records an expectation; the lambda builds a fresh
        // ValueTask per invocation, so nothing is awaited twice.
#pragma warning disable CA2012
        mediator.Send(Arg.Any<ListReportTypesQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IReadOnlyList<ReportTypeDto>>(
                new[] { new ReportTypeDto(ReportTypeId, "Initial Evaluation", 1, true) }));

        mediator.Send(Arg.Any<ListReportFieldsQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<IReadOnlyList<ReportFieldDto>>(
                new[] { new ReportFieldDto(1, ReportTypeId, "Subjective", "SOAP", 1, true, null) }));

        mediator.Send(Arg.Any<GetClinicByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ClinicDto>(new ClinicDto(
                ClinicId, "C1", "Main Street Clinic", "1 Main St", null, "Springfield", "IL",
                "62701", null, "UTC", true, DateTime.UtcNow, null, orientation)));

        // PagedResponse<T> is a class with init-only properties — build it with an initializer.
        mediator.Send(Arg.Any<ListCustomDiagnosticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<PagedResponse<CustomDiagnosticDto>>(
                new PagedResponse<CustomDiagnosticDto>
                {
                    Items = new[]
                    {
                        new CustomDiagnosticDto(DiagnosticId, "M54.5", "Low back pain", null, false, true, DateTime.UtcNow, null),
                    },
                    PageNumber = 1,
                    PageSize = 200,
                    TotalCount = 1,
                }));

        mediator.Send(Arg.Any<GetProviderByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ProviderDto>(new ProviderDto(
                Guid.NewGuid(), "Ada", "Smith", "Dr.", null, null, null, null, ClinicId,
                "Main Street Clinic", null, true, DateTime.UtcNow, null)));

        mediator.Send(Arg.Any<GetDepartmentByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<DepartmentDto>(new DepartmentDto(
                Guid.NewGuid(), "Chiropractic", 1, true, DateTime.UtcNow, null)));
#pragma warning restore CA2012
        return mediator;
    }

    private static async Task<(FSH.Modules.Patient.Domain.Patient Patient, PatientIncident Incident)> SeedPatientAndIncident(
        PatientDbContext db)
    {
        // Same minimal patient the other handler suites seed (see PatientAllergyHandlerTests).
        FSH.Modules.Patient.Domain.Patient patient = FSH.Modules.Patient.Domain.Patient.Create(
            "P-0001", true,
            PatientDemographics.Create(
                "Jane", "Doe", "A",
                new DateTime(1980, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                "F", null, isMinor: false,
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

        PatientIncident incident = PatientIncident.Create(
            patient.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
            isTransfer: false,
            isAccident: false,
            accidentType: null,
            accidentState: null,
            comments: null);
        incident.SetDiagnostics([DiagnosticId]);
        db.PatientIncidents.Add(incident);

        await db.SaveChangesAsync();
        return (patient, incident);
    }

    private static async Task<PatientReport> SeedReport(
        PatientDbContext db, Guid patientId, Guid incidentId, Guid? clinicId, DateTime reportDate, Guid? providerId)
    {
        PatientReport report = PatientReport.Create(
            incidentId, patientId, ReportTypeId, reportDate, providerId: providerId,
            clinicId: clinicId, appointmentId: null, isNoShow: false);
        db.PatientReports.Add(report);

        await db.SaveChangesAsync();
        return report;
    }

    private static async Task<PatientReport> Seed(PatientDbContext db, Guid? clinicId)
    {
        (FSH.Modules.Patient.Domain.Patient patient, PatientIncident incident) = await SeedPatientAndIncident(db);
        return await SeedReport(db, patient.Id, incident.Id, clinicId, DateTime.UtcNow.Date, Guid.NewGuid());
    }

    private static ExportPatientReportsPdfQueryHandler Sut(PatientDbContext db, IMediator mediator) =>
        new(db, mediator, new PatientReportPdfRenderer(), new PdfPasswordProtector(),
            Substitute.For<IPatientDocumentStorage>(), Substitute.For<IAuditPublisher>());

    [Fact]
    public async Task Export_Should_Use_The_Reports_Clinic_Orientation()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await Seed(db, ClinicId);
        ExportPatientReportsPdfQueryHandler sut = Sut(db, Mediator(PrintOrientation.Landscape));

        ExportedReportsPdfDto result = await sut.Handle(
            new ExportPatientReportsPdfQuery([report.Id], null), CancellationToken.None);

        using var stream = new MemoryStream(result.Content);
        using PdfDocument doc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        doc.Pages[0].Width.Point.ShouldBe(792, tolerance: 1);
    }

    [Fact]
    public async Task Export_Should_Fall_Back_To_Portrait_When_The_Report_Has_No_Clinic()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await Seed(db, clinicId: null);
        ExportPatientReportsPdfQueryHandler sut = Sut(db, Mediator(PrintOrientation.Landscape));

        ExportedReportsPdfDto result = await sut.Handle(
            new ExportPatientReportsPdfQuery([report.Id], null), CancellationToken.None);

        using var stream = new MemoryStream(result.Content);
        using PdfDocument doc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        doc.Pages[0].Width.Point.ShouldBe(612, tolerance: 1);
    }

    [Fact]
    public async Task Export_Should_Not_Fail_The_Batch_When_One_Reports_Clinic_No_Longer_Resolves()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());

        Guid deletedClinicId = Guid.NewGuid();
        Guid landscapeClinicId = Guid.NewGuid();

        (FSH.Modules.Patient.Domain.Patient patient, PatientIncident incident) = await SeedPatientAndIncident(db);
        PatientReport firstReport = await SeedReport(
            db, patient.Id, incident.Id, deletedClinicId,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());
        PatientReport secondReport = await SeedReport(
            db, patient.Id, incident.Id, landscapeClinicId,
            new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

        IMediator mediator = Mediator(PrintOrientation.Portrait);
#pragma warning disable CA2012
        // The first report's clinic has since been soft-deleted (closed) — its lookup throws,
        // just as Administration.GetClinicByIdQueryHandler does for a soft-deleted row.
        mediator.Send(Arg.Is<GetClinicByIdQuery>(q => q.Id == deletedClinicId), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ClinicDto>(
                Task.FromException<ClinicDto>(new NotFoundException($"Clinic {deletedClinicId} not found."))));
        mediator.Send(Arg.Is<GetClinicByIdQuery>(q => q.Id == landscapeClinicId), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ClinicDto>(new ClinicDto(
                landscapeClinicId, "C2", "Second Clinic", "2 Main St", null, "Springfield", "IL",
                "62701", null, "UTC", true, DateTime.UtcNow, null, PrintOrientation.Landscape)));
#pragma warning restore CA2012

        ExportPatientReportsPdfQueryHandler sut = Sut(db, mediator);

        ExportedReportsPdfDto result = await sut.Handle(
            new ExportPatientReportsPdfQuery([firstReport.Id, secondReport.Id], null), CancellationToken.None);

        using var stream = new MemoryStream(result.Content);
        using PdfDocument doc = PdfReader.Open(stream, PdfDocumentOpenMode.Import);
        doc.Pages[0].Width.Point.ShouldBe(612, tolerance: 1);
        doc.Pages[doc.PageCount - 1].Width.Point.ShouldBe(792, tolerance: 1);
    }

    [Fact]
    public async Task Export_Should_Not_Fail_When_A_Reports_Provider_No_Longer_Resolves()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());

        Guid deletedProviderId = Guid.NewGuid();
        PatientReport report = await SeedReportForDeletedProvider(db, deletedProviderId);

        IMediator mediator = Mediator(PrintOrientation.Portrait);
#pragma warning disable CA2012
        // The report's provider has since been offboarded — its lookup throws, just as
        // Administration.GetProviderByIdQueryHandler does for a soft-deleted row.
        mediator.Send(Arg.Is<GetProviderByIdQuery>(q => q.Id == deletedProviderId), Arg.Any<CancellationToken>())
            .Returns(_ => new ValueTask<ProviderDto>(
                Task.FromException<ProviderDto>(new NotFoundException($"Provider {deletedProviderId} not found."))));
#pragma warning restore CA2012

        ExportPatientReportsPdfQueryHandler sut = Sut(db, mediator);

        ExportedReportsPdfDto result = await sut.Handle(
            new ExportPatientReportsPdfQuery([report.Id], null), CancellationToken.None);

        result.Content.Length.ShouldBeGreaterThan(0);
        System.Text.Encoding.ASCII.GetString(result.Content, 0, 5).ShouldBe("%PDF-");
    }

    private static async Task<PatientReport> SeedReportForDeletedProvider(PatientDbContext db, Guid providerId)
    {
        (FSH.Modules.Patient.Domain.Patient patient, PatientIncident incident) = await SeedPatientAndIncident(db);
        return await SeedReport(db, patient.Id, incident.Id, ClinicId, DateTime.UtcNow.Date, providerId);
    }
}
