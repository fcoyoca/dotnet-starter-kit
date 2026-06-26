namespace FSH.Modules.Patient.Domain;

public sealed class PatientReportAddendum
{
    public Guid Id { get; private set; }
    public Guid ReportId { get; private set; }
    public string Text { get; private set; } = default!;
    public string CreatedByUserId { get; private set; } = default!;
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private PatientReportAddendum() { }

    public static PatientReportAddendum Create(Guid reportId, string text, string createdByUserId, string? createdByName) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            ReportId = reportId,
            Text = text,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
}
