using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using PdfSharp.Pdf.IO;
using Shouldly;

namespace Patient.Tests.Services;

public sealed class PatientReportPdfRendererTests
{
    private readonly PatientReportPdfRenderer _sut = new();

    private static ReportPdfPatientInfo SomePatient() =>
        new("Jane A Doe", "P-0001", new DateTime(1980, 4, 12, 0, 0, 0, DateTimeKind.Utc), "Female",
            DateOfInitialVisit: new DateTime(2026, 5, 4, 0, 0, 0, DateTimeKind.Utc),
            DateOfLoss: new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc),
            DiagnosisCodes: "M54.5, M99.01");

    private static ReportPdfModel SomeReport(
        string typeName = "Initial Evaluation",
        bool supportsVitals = true,
        bool isSigned = true,
        PrintOrientation orientation = PrintOrientation.Portrait) => new(
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
        Addendums: [new ReportPdfAddendum("Dr. Smith", new DateTime(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc), "Follow-up scheduled.")],
        ClinicName: "Main Street Clinic",
        Orientation: orientation,
        DepartmentName: "Chiropractic",
        ProviderName: "Dr. Smith",
        ModifiedOnUtc: null,
        SignatureImage: null,
        ReviewSignatureImage: null);

    /// <summary>Concatenates every text-showing operator in the page's content stream, decoded to
    /// readable Unicode.
    /// <para>QuestPDF always embeds subsetted composite (Type0/Identity-H) fonts — even for plain
    /// ASCII — so the raw bytes a <c>Tj</c> operator shows are glyph CIDs assigned by the font
    /// subsetter, not character codes; reading them naively (as the obvious approach would) yields
    /// garbage. Each embedded font carries a <c>/ToUnicode</c> CMap for exactly this purpose — copy/
    /// search tools use it to recover real text — so this decodes every shown string through the
    /// CMap of whichever font was most recently selected by <c>Tf</c>.</para></summary>
    private static string TextOf(PdfPage page)
    {
        Dictionary<string, Dictionary<int, char>> fontMaps = BuildFontCidToUnicodeMaps(page);
        var text = new System.Text.StringBuilder();
        string? currentFont = null;
        AppendText(ContentReader.ReadContent(page), text, fontMaps, ref currentFont);
        return text.ToString();

        static void AppendText(
            CSequence sequence,
            System.Text.StringBuilder into,
            Dictionary<string, Dictionary<int, char>> fontMaps,
            ref string? currentFont)
        {
            foreach (CObject obj in sequence)
            {
                switch (obj)
                {
                    // QuestPDF opens one "BT … ET" text object per rendered text run (a whole
                    // Span, not per glyph) but never inserts a literal space *between* runs — two
                    // adjacent labels would otherwise fuse ("Main Street ClinicInitial
                    // Evaluation"). A boundary space on every new text object is exactly the join
                    // two runs need; spaces *within* a run are already real glyphs decoded below.
                    case COperator { OpCode.OpCodeName: OpCodeName.BT }:
                        if (into.Length > 0 && into[^1] != ' ')
                        {
                            into.Append(' ');
                        }

                        break;
                    case COperator { OpCode.OpCodeName: OpCodeName.Tf } op
                        when op.Operands.Count > 0 && op.Operands[0] is CName fontName:
                        currentFont = fontName.Name;
                        break;
                    case COperator op:
                        AppendOperandStrings(op.Operands, into, fontMaps, currentFont);
                        break;
                    case CSequence nested:
                        AppendText(nested, into, fontMaps, ref currentFont);
                        break;
                    default:
                        break;
                }
            }
        }

        static void AppendOperandStrings(
            CSequence operands,
            System.Text.StringBuilder into,
            Dictionary<string, Dictionary<int, char>> fontMaps,
            string? currentFont)
        {
            foreach (CObject operand in operands)
            {
                switch (operand)
                {
                    case CString s:
                        AppendDecoded(s, into, fontMaps, currentFont);
                        break;
                    case CArray array:
                        AppendOperandStrings(array, into, fontMaps, currentFont);
                        break;
                    default:
                        break;
                }
            }
        }

        static void AppendDecoded(
            CString s,
            System.Text.StringBuilder into,
            Dictionary<string, Dictionary<int, char>> fontMaps,
            string? currentFont)
        {
            Dictionary<int, char>? map = currentFont is not null && fontMaps.TryGetValue(currentFont, out var m)
                ? m
                : null;

            // Each glyph is a 2-byte CID under the Identity-H encoding QuestPDF always emits.
            for (int i = 0; i + 1 < s.Value.Length; i += 2)
            {
                int cid = (s.Value[i] << 8) | s.Value[i + 1];
                if (map is not null && map.TryGetValue(cid, out char ch))
                {
                    into.Append(ch);
                }
            }
        }
    }

    /// <summary>Reads every font on the page's <c>/Font</c> resource dictionary and parses its
    /// <c>/ToUnicode</c> CMap stream (<c>bfchar</c> single mappings and <c>bfrange</c> contiguous
    /// runs) into a CID → Unicode lookup, keyed by the PDF resource name (e.g. <c>/F4</c>).</summary>
    private static Dictionary<string, Dictionary<int, char>> BuildFontCidToUnicodeMaps(PdfPage page)
    {
        var result = new Dictionary<string, Dictionary<int, char>>();
        if (page.Resources.Elements.GetObject("/Font") is not PdfSharp.Pdf.PdfDictionary fonts)
        {
            return result;
        }

        foreach (string name in fonts.Elements.Keys)
        {
            var map = new Dictionary<int, char>();
            if (fonts.Elements.GetObject(name) is PdfSharp.Pdf.PdfDictionary fontDict
                && fontDict.Elements.GetObject("/ToUnicode") is PdfSharp.Pdf.PdfDictionary { Stream: not null } toUnicode)
            {
                string cmap = System.Text.Encoding.Latin1.GetString(toUnicode.Stream.UnfilteredValue);
                ParseBfChar(cmap, map);
                ParseBfRange(cmap, map);
            }

            result[name] = map;
        }

        return result;
    }

    private static void ParseBfChar(string cmap, Dictionary<int, char> map)
    {
        foreach (System.Text.RegularExpressions.Match block in System.Text.RegularExpressions.Regex.Matches(
            cmap, "beginbfchar(?<body>.*?)endbfchar", System.Text.RegularExpressions.RegexOptions.Singleline))
        {
            foreach (System.Text.RegularExpressions.Match pair in System.Text.RegularExpressions.Regex.Matches(
                block.Groups["body"].Value, "<(?<cid>[0-9A-Fa-f]{4})>\\s*<(?<uni>[0-9A-Fa-f]{4})>"))
            {
                int cid = Convert.ToInt32(pair.Groups["cid"].Value, 16);
                int uni = Convert.ToInt32(pair.Groups["uni"].Value, 16);
                map[cid] = (char)uni;
            }
        }
    }

    private static void ParseBfRange(string cmap, Dictionary<int, char> map)
    {
        foreach (System.Text.RegularExpressions.Match block in System.Text.RegularExpressions.Regex.Matches(
            cmap, "beginbfrange(?<body>.*?)endbfrange", System.Text.RegularExpressions.RegexOptions.Singleline))
        {
            foreach (System.Text.RegularExpressions.Match triple in System.Text.RegularExpressions.Regex.Matches(
                block.Groups["body"].Value,
                "<(?<start>[0-9A-Fa-f]{4})>\\s*<(?<end>[0-9A-Fa-f]{4})>\\s*<(?<uni>[0-9A-Fa-f]{4})>"))
            {
                int start = Convert.ToInt32(triple.Groups["start"].Value, 16);
                int end = Convert.ToInt32(triple.Groups["end"].Value, 16);
                int uni = Convert.ToInt32(triple.Groups["uni"].Value, 16);
                for (int cid = start; cid <= end; cid++)
                {
                    map[cid] = (char)(uni + (cid - start));
                }
            }
        }
    }

    private static PdfDocument Open(byte[] pdf)
    {
        var stream = new MemoryStream(pdf);
        return PdfReader.Open(stream, PdfDocumentOpenMode.Import);
    }

    [Fact]
    public void Render_Should_Produce_Pdf_For_SingleReport()
    {
        byte[] result = _sut.Render(SomePatient(), [SomeReport()]);

        result.ShouldNotBeEmpty();
        // Every PDF starts with the %PDF- magic marker.
        result[..5].ShouldBe("%PDF-"u8.ToArray());
    }

    [Fact]
    public void Render_Should_Use_Letter_Portrait_By_Default()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport()]));

        PdfPage page = doc.Pages[0];
        // US Letter is 8.5in x 11in = 612pt x 792pt.
        page.Width.Point.ShouldBe(612, tolerance: 1);
        page.Height.Point.ShouldBe(792, tolerance: 1);
    }

    [Fact]
    public void Render_Should_Use_Landscape_When_The_Reports_Clinic_Is_Landscape()
    {
        using PdfDocument doc = Open(
            _sut.Render(SomePatient(), [SomeReport(orientation: PrintOrientation.Landscape)]));

        PdfPage page = doc.Pages[0];
        page.Width.Point.ShouldBe(792, tolerance: 1);
        page.Height.Point.ShouldBe(612, tolerance: 1);
    }

    [Fact]
    public void Render_Should_Honour_Each_Reports_Own_Orientation_In_One_Document()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(),
        [
            SomeReport(orientation: PrintOrientation.Portrait),
            SomeReport(typeName: "Progress", orientation: PrintOrientation.Landscape),
        ]));

        doc.Pages[0].Width.Point.ShouldBe(612, tolerance: 1);
        doc.Pages[^1].Width.Point.ShouldBe(792, tolerance: 1);
    }

    [Fact]
    public void Render_Should_Print_The_Legacy_Header_Fields()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport()]));

        string text = TextOf(doc.Pages[0]);
        text.ShouldContain("Main Street Clinic");
        text.ShouldContain("Initial Evaluation");
        text.ShouldContain("Jane A Doe");
        text.ShouldContain("DOIV");
        text.ShouldContain("DOL");
        text.ShouldContain("M54.5");
    }

    [Fact]
    public void Render_Should_Print_The_Digitally_Signed_Line()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport()]));

        TextOf(doc.Pages[0]).ShouldContain("digitally signed by");
    }

    [Fact]
    public void Render_Should_Watermark_An_Unsigned_Report()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport(isSigned: false)]));

        TextOf(doc.Pages[0]).ShouldContain("DRAFT");
    }

    [Fact]
    public void Render_Should_Print_Vitals_Even_When_No_Clinical_Exam_Section_Exists()
    {
        // Mirrors a real report: Height/Weight/BP were recorded via the dedicated vitals inputs,
        // but the free-text "Comments" field under Clinical Exam was left blank, so
        // PatientReport.SetFieldValues never created a section for that category.
        ReportPdfModel report = SomeReport() with
        {
            Sections =
            [
                new ReportPdfSection("Subjective", "SOAP", "Patient reports lower back pain."),
            ],
        };

        using PdfDocument doc = Open(_sut.Render(SomePatient(), [report]));

        string text = TextOf(doc.Pages[0]);
        text.ShouldContain("Clinical Exam");
        text.ShouldContain("Height");
        text.ShouldContain("66");
        text.ShouldContain("BP");
        text.ShouldContain("120/80");
    }

    [Fact]
    public void Render_Should_Print_Vitals_Once_When_A_Clinical_Exam_Section_Has_Text()
    {
        ReportPdfModel report = SomeReport() with
        {
            Sections =
            [
                new ReportPdfSection("Comments", "Clinical Exam", "Patient tolerated exam well."),
            ],
        };

        using PdfDocument doc = Open(_sut.Render(SomePatient(), [report]));

        string text = TextOf(doc.Pages[0]);
        text.ShouldContain("Height");
        text.ShouldContain("66");
        text.ShouldContain("BP");
        text.ShouldContain("120/80");
        text.ShouldContain("Patient tolerated exam well.");

        int firstIndex = text.IndexOf("Clinical Exam", StringComparison.Ordinal);
        firstIndex.ShouldBeGreaterThanOrEqualTo(0);
        text.IndexOf("Clinical Exam", firstIndex + 1, StringComparison.Ordinal).ShouldBe(-1);
    }

    [Fact]
    public void Render_Should_Not_Print_Vitals_When_The_Report_Type_Does_Not_Support_Them()
    {
        using PdfDocument doc = Open(_sut.Render(SomePatient(), [SomeReport(supportsVitals: false)]));

        string text = TextOf(doc.Pages[0]);
        text.ShouldNotContain("Height");
        text.ShouldNotContain("BP");
    }
}
