namespace FSH.Modules.Administration.Data;

/// <summary>
/// The standard BackChart report-template catalog (6 report types + their fields), migrated from the legacy
/// <c>ReportTypes</c>/<c>ReportFields</c> tables. Seeded per-tenant on first run by
/// <see cref="AdministrationDbInitializer"/>; tenants then manage their own copies. Types 1-3 share the same field
/// set in legacy — here each type gets its own copy (a field belongs to one type).
/// </summary>
internal static class ReportTemplateSeedData
{
    public sealed record SeedField(string Name, string? Category, int Order, bool IsActive, int LegacyId);

    public sealed record SeedType(string Name, int LegacyId, int Order, IReadOnlyList<SeedField> Fields);

    // Shared by Initial Evaluation / Progress Report / Discharge Report in legacy.
    // Order is the type's GLOBAL display sequence (legacy category rcOrder, then rfOrder within
    // the category) — the editor and PDF sort by it alone, so it must be unique per type.
    private static readonly IReadOnlyList<SeedField> EvaluationFields =
    [
        new("Chief Complaint", "Chief Complaint", 1, true, 1),
        new("Present Problem", "Present Problem", 2, true, 2),
        new("Medical History", "Medical History", 3, true, 3),
        new("Family History", "Family History", 4, true, 30),
        new("Personal / Social History", "Personal / Social History", 5, true, 4),
        new("Allergies", "Allergies", 6, true, 5),
        new("Medications", "Medications", 7, true, 6),
        new("Systems Review", "Systems Review", 8, true, 7),
        new("Comments", "Clinical Exam", 9, true, 13),
        new("Diagnostic Imaging", "Diagnostic Imaging", 10, true, 14),
        new("Clinical Impression", "Clinical Impression", 11, true, 15),
        new("Short Term Goals", "Goals", 12, true, 27),
        new("Long Term Goals", "Goals", 13, true, 28),
        new("Plan", "Plan", 14, true, 24), // legacy "Therapeutic Care" slot (rcOrder 13)
        new("Work Status or Restrictions", "Work Status or Restrictions", 15, true, 29),
    ];

    public static IReadOnlyList<SeedType> Types { get; } =
    [
        new("Initial Evaluation", 1, 1, EvaluationFields),
        new("Progress Report", 2, 2, EvaluationFields),
        new("Discharge Report", 3, 3, EvaluationFields),
        new("Daily Visit", 4, 4,
        [
            // Legacy SOAP order: Subjective → Objective → Assessment → Plan.
            new("ADL", "Subjective", 1, false, 17),
            new("Pain", "Subjective", 2, false, 18),
            new("Subjective", "Subjective", 3, true, 19),
            new("Objective", "Objective", 4, true, 20),
            new("Assessment", "Assessment", 5, true, 22),
            new("Niall Radio Group", "Assessment", 6, false, 31),
            new("Niall's yes/no", "Assessment", 7, false, 33),
            new("Niall's drop down", "Assessment", 8, false, 34),
            new("Plan", "Plan", 9, true, 24),
        ]),
        new("No Show", 5, 5,
        [
            new("Comments", "Comments", 1, true, 38),
        ]),
        new("NoFieldReport", 6, 6,
        [
            new("Documentation", "Documentation", 1, true, 39),
        ]),
    ];
}
