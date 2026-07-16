namespace FSH.Modules.Patient.Domain;

public sealed class PatientReportFieldValue
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public int ReportFieldId { get; private set; }
    public string Text { get; private set; } = default!;

    private PatientReportFieldValue() { }

    public static PatientReportFieldValue Create(Guid reportId, int reportFieldId, string text) =>
        new() { Id = Guid.CreateVersion7(), ReportId = reportId, ReportFieldId = reportFieldId, Text = text };

    /// <summary>Updates the text of an existing value in place (keeps the row's key so EF issues a
    /// real UPDATE rather than a phantom insert/delete on collection replace).</summary>
    public void UpdateText(string text) => Text = text;
}
