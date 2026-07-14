using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.IO;
using Shouldly;

namespace Patient.Tests.Services;

public sealed class PatientReportPdfRendererTests
{
    private readonly PatientReportPdfRenderer _sut = new();

    private static ReportPdfPatientInfo SomePatient() =>
        new("Jane A Doe", "P-0001", new DateTime(1980, 4, 12, 0, 0, 0, DateTimeKind.Utc), "Female");

    private static ReportPdfModel SomeReport(
        string typeName = "Initial Evaluation",
        bool supportsVitals = true,
        bool isSigned = true) => new(
        ReportTypeName: typeName,
        ReportDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
        Version: 1,
        IsNoShow: false,
        WorkflowStatus: isSigned ? "Signed" : "Draft",
        IsSigned: isSigned,
        Vitals: new ReportVitalsDto(66m, 150m, 24.2m, 120, 80, 72, 98.6m),
        SupportsVitals: supportsVitals,
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
    public void Render_Should_Produce_Pdf_When_Type_Does_Not_Support_Vitals()
    {
        // Daily Visit / No Show reports never print vitals even when values were
        // captured — the renderer must skip the vitals block without failing.
        byte[] result = _sut.Render(SomePatient(), [SomeReport("Daily Visit", supportsVitals: false)]);

        result.ShouldNotBeEmpty();
        result[..5].ShouldBe("%PDF-"u8.ToArray());
    }

    [Fact]
    public void Render_Should_Throw_When_NoReports()
    {
        Should.Throw<ArgumentException>(() => _sut.Render(SomePatient(), []));
    }

    [Fact]
    public void Render_Should_Add_Watermark_Content_To_Unsigned_Reports()
    {
        // The DRAFT watermark is drawn as glyphs from a subsetted font, so it cannot be asserted on
        // as extractable text. What is assertable is that IsSigned reaches the rendered output at
        // all: an unsigned report draws strictly more page content than the same report signed.
        // Whether that content *reads* "DRAFT — UNSIGNED" is confirmed by looking at the PDF.
        byte[] draft = _sut.Render(SomePatient(), [SomeReport(isSigned: false)]);
        byte[] signed = _sut.Render(SomePatient(), [SomeReport(isSigned: true)]);

        PageContentLength(draft).ShouldBeGreaterThan(PageContentLength(signed));
    }

    /// <summary>Total length of the page content streams — the drawing instructions themselves,
    /// independent of PDF-level metadata and object numbering.</summary>
    private static int PageContentLength(byte[] pdf)
    {
        using var stream = new MemoryStream(pdf, writable: false);
        using PdfDocument document = PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        return document.Pages
            .Cast<PdfPage>()
            .Sum(page => ContentReader.ReadContent(page).ToString()!.Length);
    }
}
