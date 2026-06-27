using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Contracts.Dtos;

namespace FSH.Modules.Patient.Domain;

/// <summary>
/// A patient's problem-list entry (legacy <c>PatientProblems</c>). Each problem is a diagnosis: it
/// references a diagnostic (<see cref="DiagnosticId"/>, bare cross-schema id to the Administration
/// catalog) and snapshots the diagnostic's code + description so the list renders without a cross-module
/// read (legacy stored <c>pptName</c>/<c>ldxDescription</c> on the row too). Tracks a clinical
/// <see cref="Status"/>, a diagnosis date, free-text notes, and a medical-alert flag. Soft-deletable.
/// </summary>
public sealed class PatientProblem : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid PatientId { get; private set; }

    /// <summary>Optional incident this problem was raised under (legacy <c>ppIncidentID</c>).</summary>
    public Guid? IncidentId { get; private set; }

    /// <summary>The ICD diagnostic this problem is for (bare id into the Administration global diagnostics
    /// catalog — legacy <c>ppPatientProblemTypeID</c> = <c>ldxID</c>).</summary>
    public int DiagnosticId { get; private set; }

    /// <summary>Snapshot of the diagnostic code at create/update time (legacy <c>pptName</c>).</summary>
    public string DiagnosticCode { get; private set; } = default!;

    /// <summary>Snapshot of the diagnostic description (legacy <c>ldxDescription</c>).</summary>
    public string? DiagnosticDescription { get; private set; }

    public DateTime? DiagnosisDate { get; private set; }
    public ProblemStatus Status { get; private set; }
    public string? Notes { get; private set; }
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

    private PatientProblem() { }

    public static PatientProblem Create(
        Guid patientId,
        int diagnosticId,
        string diagnosticCode,
        string? diagnosticDescription,
        DateTime? diagnosisDate,
        ProblemStatus status,
        string? notes,
        bool isMedicalAlert,
        Guid? incidentId,
        string? createdByUserId,
        string? createdByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticCode);
        return new PatientProblem
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            DiagnosticId = diagnosticId,
            DiagnosticCode = diagnosticCode.Trim(),
            DiagnosticDescription = string.IsNullOrWhiteSpace(diagnosticDescription) ? null : diagnosticDescription.Trim(),
            DiagnosisDate = diagnosisDate,
            Status = status,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
            IsMedicalAlert = isMedicalAlert,
            IncidentId = incidentId,
            CreatedByUserId = createdByUserId,
            CreatedByName = createdByName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        int diagnosticId,
        string diagnosticCode,
        string? diagnosticDescription,
        DateTime? diagnosisDate,
        ProblemStatus status,
        string? notes,
        bool isMedicalAlert,
        string? updatedByUserId,
        string? updatedByName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticCode);
        DiagnosticId = diagnosticId;
        DiagnosticCode = diagnosticCode.Trim();
        DiagnosticDescription = string.IsNullOrWhiteSpace(diagnosticDescription) ? null : diagnosticDescription.Trim();
        DiagnosisDate = diagnosisDate;
        Status = status;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;
        IsMedicalAlert = isMedicalAlert;
        Touch(updatedByUserId, updatedByName);
    }

    public void SetStatus(ProblemStatus status, string? updatedByUserId, string? updatedByName)
    {
        Status = status;
        Touch(updatedByUserId, updatedByName);
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
    }

    private void Touch(string? userId, string? name)
    {
        UpdatedByUserId = userId;
        UpdatedByName = name;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
