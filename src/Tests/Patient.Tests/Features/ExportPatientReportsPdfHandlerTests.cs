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

    private static async Task<PatientReport> Seed(PatientDbContext db, Guid? clinicId)
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

        PatientReport report = PatientReport.Create(
            incident.Id, patient.Id, ReportTypeId, DateTime.UtcNow.Date, providerId: Guid.NewGuid(),
            clinicId: clinicId, appointmentId: null, isNoShow: false);
        db.PatientReports.Add(report);

        await db.SaveChangesAsync();
        return report;
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
}
