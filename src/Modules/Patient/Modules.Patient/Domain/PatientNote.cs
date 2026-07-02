using FSH.Framework.Core.Domain;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A free-form patient chart note (legacy <c>PatientNotes</c>: <c>pnName</c>/<c>pnDescription</c>/
/// <c>pnMedicalAlert</c>/<c>pnDeleted</c>). Notes flagged <see cref="IsMedicalAlert"/> surface in the
/// chart's Medical Alerts banner alongside alert-flagged problems. Soft-deletable (legacy pnDeleted).
/// </summary>
public sealed class PatientNote : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid PatientId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsMedicalAlert { get; private set; }

    public string? CreatedByUserId { get; private set; }
    public string? CreatedByName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? UpdatedByUserId { get; private set; }
    public string? UpdatedByName { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    private PatientNote() { }

    public static PatientNote Create(
        Guid patientId,
        string name,
        string? description,
        bool isMedicalAlert,
        string? createdByUserId,
        string? createdByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new PatientNote
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsMedicalAlert = isMedicalAlert,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string name,
        string? description,
        bool isMedicalAlert,
        string? updatedByUserId,
        string? updatedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsMedicalAlert = isMedicalAlert;
        UpdatedByUserId = updatedByUserId;
        UpdatedByName = updatedByName;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }
}
