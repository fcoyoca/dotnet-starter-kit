using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Shouldly;

namespace Patient.Tests.Services;

public sealed class PdfPasswordProtectorTests
{
    private readonly PdfPasswordProtector _sut = new();
    private readonly PatientReportPdfRenderer _renderer = new();

    private const string Password = "correct horse battery staple";

    /// <summary>A real rendered report, not a hand-made PDF — the point of these tests is that the
    /// protector can round-trip what QuestPDF actually emits.</summary>
    private byte[] RenderedReport() => _renderer.Render(
        new ReportPdfPatientInfo("Jane A Doe", "P-0001", new DateTime(1980, 4, 12, 0, 0, 0, DateTimeKind.Utc), "Female"),
        [
            new ReportPdfModel(
                ReportTypeName: "Initial Evaluation",
                ReportDate: new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Version: 1,
                IsNoShow: false,
                WorkflowStatus: "Signed",
                IsSigned: true,
                Vitals: new ReportVitalsDto(66m, 150m, 24.2m, 120, 80, 72, 98.6m),
                SupportsVitals: true,
                Sections: [new ReportPdfSection("Subjective", "SOAP", "Patient reports lower back pain.")],
                SignedByName: "Dr. Smith",
                SignedOnUtc: new DateTime(2026, 6, 1, 15, 30, 0, DateTimeKind.Utc),
                ReviewSignedByName: null,
                ReviewSignedOnUtc: null,
                Addendums: []),
        ]);

    [Fact]
    public void Protect_Should_Produce_A_Pdf_That_Requires_The_Password()
    {
        byte[] result = _sut.Protect(RenderedReport(), Password);

        result[..5].ShouldBe("%PDF-"u8.ToArray());

        // Opening without a password must fail — otherwise the "protected" export protects nothing.
        using var stream = new MemoryStream(result, writable: false);
        Should.Throw<PdfReaderException>(() => PdfReader.Open(stream, PdfDocumentOpenMode.Import));
    }

    [Fact]
    public void Protect_Should_Produce_A_Pdf_That_Opens_With_The_Password()
    {
        byte[] result = _sut.Protect(RenderedReport(), Password);

        // Parsing the pages back out is the proof it decrypted: PdfReader would have thrown on a
        // bad password, and could not enumerate pages if the AES payload had not been unwrapped.
        using var stream = new MemoryStream(result, writable: false);
        using PdfDocument document = PdfReader.Open(stream, Password, PdfDocumentOpenMode.Import);

        document.PageCount.ShouldBe(1);
    }

    [Fact]
    public void Protect_Should_Produce_A_Pdf_That_Rejects_The_Wrong_Password()
    {
        byte[] result = _sut.Protect(RenderedReport(), Password);

        using var stream = new MemoryStream(result, writable: false);
        Should.Throw<PdfReaderException>(
            () => PdfReader.Open(stream, "not-the-password", PdfDocumentOpenMode.Import));
    }

    [Fact]
    public void Protect_Should_Reject_A_Blank_Password()
    {
        // The caller decides whether to protect at all; if it says yes, it must mean it.
        Should.Throw<ArgumentException>(() => _sut.Protect(RenderedReport(), "   "));
    }
}
