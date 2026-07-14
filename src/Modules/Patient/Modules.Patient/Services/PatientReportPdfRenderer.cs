using System.Globalization;
using FSH.Modules.Patient.Contracts.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FSH.Modules.Patient.Services;

/// <summary>
/// QuestPDF-based patient-report renderer (mirrors Billing's <c>InvoicePdfRenderer</c>). QuestPDF's
/// Community license is free for organisations under $1M USD/year revenue; larger downstream users
/// must obtain a license. Isolated behind <see cref="IPatientReportPdfRenderer"/> so it can be
/// swapped without touching callers.
/// </summary>
public sealed class PatientReportPdfRenderer : IPatientReportPdfRenderer
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    static PatientReportPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(ReportPdfPatientInfo patient, IReadOnlyList<ReportPdfModel> reports)
    {
        ArgumentNullException.ThrowIfNull(patient);
        ArgumentNullException.ThrowIfNull(reports);
        if (reports.Count == 0)
        {
            throw new ArgumentException("At least one report is required.", nameof(reports));
        }

        return Document.Create(container =>
        {
            foreach (ReportPdfModel report in reports)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                    // An unsigned report is not a finalised clinical record. It can still be
                    // printed, but every page says so — a printout that outlives the draft must
                    // not read as the signed note.
                    if (!report.IsSigned)
                    {
                        page.Foreground()
                            .AlignCenter()
                            .AlignMiddle()
                            .Rotate(-45)
                            .Text("DRAFT — UNSIGNED")
                            .FontSize(60).Bold()
                            .FontColor(Colors.Red.Lighten4);
                    }

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text(report.ReportTypeName).FontSize(18).Bold();
                                c.Item().Text($"Report Date: {FormatDate(report.ReportDate)}")
                                    .FontSize(11).FontColor(Colors.Grey.Darken1);
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text(patient.FullName).FontSize(12).SemiBold();
                                c.Item().Text($"Code: {patient.PatientCode}").FontColor(Colors.Grey.Darken1);
                                if (patient.DateOfBirth is { } dob)
                                {
                                    c.Item().Text($"DOB: {FormatDate(dob)}").FontColor(Colors.Grey.Darken1);
                                }

                                if (!string.IsNullOrWhiteSpace(patient.Gender))
                                {
                                    c.Item().Text($"Gender: {patient.Gender}").FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"Status: {report.WorkflowStatus}").SemiBold();
                            row.RelativeItem().AlignCenter().Text($"Version: {report.Version}");
                            row.RelativeItem().AlignRight()
                                .Text(report.IsNoShow ? "NO SHOW" : string.Empty)
                                .FontColor(Colors.Red.Darken2).SemiBold();
                        });

                        if (report.SupportsVitals && HasAnyVital(report.Vitals))
                        {
                            col.Item().Element(c => ComposeVitals(c, report.Vitals));
                        }

                        foreach (ReportPdfSection section in report.Sections)
                        {
                            col.Item().Column(c =>
                            {
                                string title = string.IsNullOrWhiteSpace(section.Category)
                                    ? section.Name
                                    : $"{section.Category} — {section.Name}";
                                c.Item().Text(title).FontSize(11).SemiBold();
                                c.Item().PaddingTop(2).Text(section.Text);
                            });
                        }

                        if (report.SignedByName is not null || report.SignedOnUtc is not null
                            || report.ReviewSignedByName is not null || report.ReviewSignedOnUtc is not null)
                        {
                            col.Item().PaddingTop(8).Column(c =>
                            {
                                c.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                                if (report.SignedByName is not null || report.SignedOnUtc is not null)
                                {
                                    c.Item().PaddingTop(4)
                                        .Text($"Electronically signed by {report.SignedByName ?? "—"} on {FormatDateTime(report.SignedOnUtc)}")
                                        .Italic();
                                }

                                if (report.ReviewSignedByName is not null || report.ReviewSignedOnUtc is not null)
                                {
                                    c.Item().PaddingTop(2)
                                        .Text($"Reviewed and signed by {report.ReviewSignedByName ?? "—"} on {FormatDateTime(report.ReviewSignedOnUtc)}")
                                        .Italic();
                                }
                            });
                        }

                        if (report.Addendums.Count > 0)
                        {
                            col.Item().PaddingTop(8).Column(c =>
                            {
                                c.Item().Text("Addendums").FontSize(11).SemiBold();
                                foreach (ReportPdfAddendum addendum in report.Addendums)
                                {
                                    c.Item().PaddingTop(4).Column(a =>
                                    {
                                        a.Item()
                                            .Text($"{addendum.CreatedByName ?? "Unknown"} · {FormatDateTime(addendum.CreatedAtUtc)}")
                                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                                        a.Item().Text(addendum.Text);
                                    });
                                }
                            });
                        }
                    });

                    page.Footer().Row(row =>
                    {
                        row.RelativeItem()
                            .Text($"Generated {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", Culture)}")
                            .FontSize(8).FontColor(Colors.Grey.Medium);
                        row.RelativeItem().AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                            t.CurrentPageNumber();
                            t.Span(" / ");
                            t.TotalPages();
                        });
                    });
                });
            }
        }).GeneratePdf();
    }

    private static void ComposeVitals(IContainer container, ReportVitalsDto vitals)
    {
        container.Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(6).Row(row =>
        {
            AddVital(row, "Height (in)", vitals.HeightInches?.ToString("0.##", Culture));
            AddVital(row, "Weight (lbs)", vitals.WeightLbs?.ToString("0.##", Culture));
            AddVital(row, "BMI", vitals.Bmi?.ToString("0.##", Culture));
            AddVital(row, "BP", vitals.Systolic is null && vitals.Diastolic is null
                ? null
                : $"{vitals.Systolic?.ToString(Culture) ?? "—"}/{vitals.Diastolic?.ToString(Culture) ?? "—"}");
            AddVital(row, "Pulse", vitals.Pulse?.ToString(Culture));
            AddVital(row, "Temp (°F)", vitals.TemperatureF?.ToString("0.#", Culture));
        });
    }

    private static void AddVital(RowDescriptor row, string label, string? value)
    {
        if (value is null)
        {
            return;
        }

        row.RelativeItem().Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
            c.Item().Text(value).SemiBold();
        });
    }

    private static bool HasAnyVital(ReportVitalsDto vitals) =>
        vitals.HeightInches is not null || vitals.WeightLbs is not null || vitals.Bmi is not null
        || vitals.Systolic is not null || vitals.Diastolic is not null || vitals.Pulse is not null
        || vitals.TemperatureF is not null;

    private static string FormatDate(DateTime value) => value.ToString("MM/dd/yyyy", Culture);

    private static string FormatDateTime(DateTime? value) =>
        value is null ? "—" : value.Value.ToString("MM/dd/yyyy HH:mm 'UTC'", Culture);
}
