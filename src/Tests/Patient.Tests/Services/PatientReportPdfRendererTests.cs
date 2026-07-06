using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Services;
using Shouldly;

namespace Patient.Tests.Services;

public sealed class PatientReportPdfRendererTests
{
    private readonly PatientReportPdfRenderer _sut = new();

    private static ReportPdfPatientInfo SomePatient() =>
        new("Jane A Doe", "P-0001", new DateTime(1980, 4, 12, 0, 0, 0, DateTimeKind.Utc), "Female");

    private static ReportPdfModel SomeReport(string typeName = "Initial Evaluation") => new(
        ReportTypeName: typeName,
        ReportDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        Version: 1,
        IsNoShow: false,
        WorkflowStatus: "Signed",
        Vitals: new ReportVitalsDto(66m, 150m, 24.2m, 120, 80, 72, 98.6m),
        Sections:
        [
            new ReportPdfSection("Subjective", "SOAP", "Patient reports lower back pain."),
            new ReportPdfSection("Objective", "SOAP", "Reduced lumbar range of motion."),
        ],
        SignedByName: "Dr. Smith",
        SignedOnUtc: new DateTime(2026, 6, 1, 15, 30, 0, DateTimeKind.Utc),
        ReviewSignedByName: null,
        ReviewSignedOnUtc: null,
        Addendums: [new ReportPdfAddendum("Dr. Smith", new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc), "Follow-up scheduled.")]);

    [Fact]
    public void Render_Should_Produce_Pdf_For_SingleReport()
    {
        byte[] result = _sut.Render(SomePatient(), [SomeReport()]);

        result.ShouldNotBeEmpty();
        // Every PDF starts with the %PDF- magic marker.
        result[..5].ShouldBe("%PDF-"u8.ToArray());
    }

    [Fact]
    public void Render_Should_Produce_Pdf_For_MultipleReports()
    {
        byte[] result = _sut.Render(SomePatient(), [SomeReport(), SomeReport("Progress Report"), SomeReport("Daily Visit")]);

        result.ShouldNotBeEmpty();
        result[..5].ShouldBe("%PDF-"u8.ToArray());
    }

    [Fact]
    public void Render_Should_Throw_When_NoReports()
    {
        Should.Throw<ArgumentException>(() => _sut.Render(SomePatient(), []));
    }
}
