using System.Globalization;
using FSH.Modules.Administration.Contracts.Dtos;
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
                    // Legacy BCFileGeneration used Letter; orientation is the report's clinic setting.
                    page.Size(report.Orientation == PrintOrientation.Landscape
                        ? PageSizes.Letter.Landscape()
                        : PageSizes.Letter.Portrait());
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

                    page.Header().Element(c => ComposeHeader(c, patient, report));
                    page.Content().PaddingVertical(12).Element(c => ComposeBody(c, report));
                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(9).FontColor(Colors.Grey.Medium));
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            }
        }).GeneratePdf();
    }

    /// <summary>Repeats on every page of a report — the legacy PDF template: clinic identity, the
    /// report title, and the patient block (Patient / DOB / DOIV / DOL / DX). The logo slot legacy
    /// drew top-left is intentionally empty: clinic-app has no logo storage yet.</summary>
    private static void ComposeHeader(IContainer container, ReportPdfPatientInfo patient, ReportPdfModel report)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(report.ClinicName ?? string.Empty)
                .FontSize(13).Bold();
            col.Item().AlignCenter().Text(report.ReportTypeName).FontSize(12).Bold();

            col.Item().PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Patient: ").SemiBold();
                    t.Span(patient.FullName);
                });
                row.ConstantItem(150).Text(t =>
                {
                    t.Span("DOB: ").SemiBold();
                    t.Span(patient.DateOfBirth is { } dob ? FormatDate(dob) : "—");
                });
                row.ConstantItem(120).Text(t =>
                {
                    t.Span("Code: ").SemiBold();
                    t.Span(patient.PatientCode);
                });
            });

            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("DOIV: ").SemiBold();
                    t.Span(patient.DateOfInitialVisit is { } doiv ? FormatDate(doiv) : "—");
                });
                row.RelativeItem().Text(t =>
                {
                    t.Span("DOL: ").SemiBold();
                    t.Span(patient.DateOfLoss is { } dol ? FormatDate(dol) : "—");
                });
            });

            col.Item().Text(t =>
            {
                t.Span("DX: ").SemiBold();
                t.Span(string.IsNullOrWhiteSpace(patient.DiagnosisCodes) ? "—" : patient.DiagnosisCodes);
            });

            col.Item().PaddingTop(6).LineHorizontal(0.75f).LineColor(Colors.Grey.Lighten1);
        });
    }

    /// <summary>The legacy report body: date line, then fields grouped category → field → text,
    /// with vitals rendered inline under the Clinical Exam category, then addendums, then the
    /// signature images and their "digitally signed by" lines.</summary>
    private static void ComposeBody(IContainer container, ReportPdfModel report)
    {
        container.Column(col =>
        {
            col.Spacing(8);

            col.Item().Text(t =>
            {
                t.Span(FormatDate(report.ReportDate)).SemiBold();
                if (report.ModifiedOnUtc is { } modified)
                {
                    t.Span($"  (Modified: {FormatDate(modified)})").FontColor(Colors.Grey.Darken1);
                }

                string who = report.ProviderName ?? report.DepartmentName ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(who))
                {
                    t.Span($"        {who}:");
                }
            });

            if (report.IsNoShow)
            {
                col.Item().Text("NO SHOW").FontColor(Colors.Red.Darken2).SemiBold();
            }

            // Vitals are recorded via dedicated inputs, independent of the free-text Clinical Exam
            // "Comments" field — PatientReport.SetFieldValues drops blank text, so a report can have
            // vitals but no "Clinical Exam" section at all. Vitals must still print whenever the
            // report type supports them and any were recorded, so a pending flag tracks whether
            // they've been emitted yet: inline under the section's own heading when that section
            // exists, or under a synthesized heading up front when it doesn't. Either way, exactly
            // once.
            bool vitalsPending = report.SupportsVitals && HasAnyVital(report.Vitals);
            bool hasClinicalExamSection = report.Sections.Any(
                s => string.Equals(s.Category, VitalsCategory, StringComparison.OrdinalIgnoreCase));

            if (vitalsPending && !hasClinicalExamSection)
            {
                col.Item().PaddingTop(4).Text(VitalsCategory).FontSize(12).Bold();
                col.Item().Element(c => ComposeVitals(c, report.Vitals));
                vitalsPending = false;
            }

            string? lastCategory = null;
            foreach (ReportPdfSection section in report.Sections)
            {
                string category = section.Category ?? string.Empty;
                if (!string.Equals(category, lastCategory, StringComparison.Ordinal))
                {
                    col.Item().PaddingTop(4).Text(category).FontSize(12).Bold();
                    lastCategory = category;

                    // Legacy printed vitals at the head of the Clinical Exam category.
                    if (vitalsPending && string.Equals(category, VitalsCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        col.Item().Element(c => ComposeVitals(c, report.Vitals));
                        vitalsPending = false;
                    }
                }

                if (string.IsNullOrWhiteSpace(section.Text))
                {
                    continue;
                }

                col.Item().Column(c =>
                {
                    c.Item().Text(section.Name).SemiBold();
                    c.Item().Text(section.Text);
                });
            }

            foreach (ReportPdfAddendum addendum in report.Addendums)
            {
                col.Item().PaddingTop(4).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Addendum ").Bold();
                        t.Span($"({addendum.CreatedByName ?? "Unknown"} — {FormatDateTime(addendum.CreatedAtUtc)})")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
                    c.Item().Text(addendum.Text);
                });
            }

            if (report.SignedByName is not null || report.SignedOnUtc is not null)
            {
                col.Item().PaddingTop(10).Element(c => ComposeSignature(
                    c, report.SignatureImage, report.SignedByName, report.SignedOnUtc));
            }

            if (report.ReviewSignedByName is not null || report.ReviewSignedOnUtc is not null)
            {
                col.Item().PaddingTop(6).Element(c => ComposeSignature(
                    c, report.ReviewSignatureImage, report.ReviewSignedByName, report.ReviewSignedOnUtc));
            }
        });
    }

    /// <summary>Signature image (when the stored file was readable) above the legacy attestation
    /// line. A missing image degrades to the line alone — the attestation is the record, the
    /// picture is decoration.</summary>
    private static void ComposeSignature(IContainer container, byte[]? image, string? name, DateTime? signedOn)
    {
        container.Column(col =>
        {
            if (image is { Length: > 0 })
            {
                col.Item().Height(40).Image(image).FitHeight();
            }

            col.Item().Text(
                $"(This report was digitally signed by {name ?? "—"} on {FormatDateTime(signedOn)})")
                .Italic();
        });
    }

    /// <summary>Legacy report category that carries vitals (rcID 8).</summary>
    private const string VitalsCategory = "Clinical Exam";

    private static void ComposeVitals(IContainer container, ReportVitalsDto vitals)
    {
        container.PaddingBottom(4).Column(col =>
        {
            AddVital(col, "Height", vitals.HeightInches is { } h ? $"{h.ToString("0.##", Culture)} in." : null);
            AddVital(col, "Weight", vitals.WeightLbs is { } w ? $"{w.ToString("0.##", Culture)} lbs." : null);
            AddVital(col, "BMI", vitals.Bmi?.ToString("0.##", Culture));
            AddVital(col, "BP", vitals.Systolic is null && vitals.Diastolic is null
                ? null
                : $"{vitals.Systolic?.ToString(Culture) ?? "—"}/{vitals.Diastolic?.ToString(Culture) ?? "—"}");
            AddVital(col, "Heart Rate", vitals.Pulse?.ToString(Culture));
            AddVital(col, "Temperature", vitals.TemperatureF is { } t ? $"{t.ToString("0.#", Culture)} °F" : null);
        });
    }

    private static void AddVital(ColumnDescriptor col, string label, string? value)
    {
        if (value is null)
        {
            return;
        }

        col.Item().Text(t =>
        {
            t.Span($"{label}: ").SemiBold();
            t.Span(value);
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
