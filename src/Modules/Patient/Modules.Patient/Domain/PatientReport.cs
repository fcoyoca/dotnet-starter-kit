using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Contracts.Dtos;

namespace FSH.Modules.Patient.Domain;

public sealed class PatientReport : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid IncidentId { get; private set; }
    public Guid PatientId { get; private set; }
    public int ReportTypeId { get; private set; }
    public DateTime ReportDate { get; private set; }
    public int Version { get; private set; }
    public Guid? ProviderId { get; private set; }
    public Guid? ClinicId { get; private set; }
    /// <summary>Optional link to the scheduling appointment this report documents
    /// (legacy BackChart <c>RAppointmentID</c>). Bare id — no FK to the Scheduling schema.</summary>
    public Guid? AppointmentId { get; private set; }
    public bool IsNoShow { get; private set; }

    public PatientReportVitals Vitals { get; private set; } = new();

    public ReportWorkflowStatus WorkflowStatus { get; private set; }
    public bool IsSigned { get; private set; }
    public string? SignedByUserId { get; private set; }
    public string? SignedByName { get; private set; }
    public DateTime? SignedOnUtc { get; private set; }
    public string? SignatureImagePath { get; private set; }

    public string? ReviewRequestedByUserId { get; private set; }
    public DateTime? ReviewRequestedOnUtc { get; private set; }
    public Guid? ReviewerProviderId { get; private set; }
    public string? ReviewSignedByUserId { get; private set; }
    public string? ReviewSignedByName { get; private set; }
    public DateTime? ReviewSignedOnUtc { get; private set; }
    public string? ReviewSignatureImagePath { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public IReadOnlyList<PatientReportFieldValue> FieldValues => _fieldValues.AsReadOnly();
    private readonly List<PatientReportFieldValue> _fieldValues = [];
    public IReadOnlyList<PatientReportAddendum> Addendums => _addendums.AsReadOnly();
    private readonly List<PatientReportAddendum> _addendums = [];
    public IReadOnlyList<PatientReportProblem> AssociatedProblems => _associatedProblems.AsReadOnly();
    private readonly List<PatientReportProblem> _associatedProblems = [];

    private PatientReport() { }

    public static PatientReport Create(
        Guid incidentId, Guid patientId, int reportTypeId, DateTime reportDate,
        Guid? providerId, Guid? clinicId, bool isNoShow, Guid? appointmentId = null)
    {
        return new PatientReport
        {
            Id = Guid.CreateVersion7(),
            IncidentId = incidentId,
            PatientId = patientId,
            ReportTypeId = reportTypeId,
            ReportDate = reportDate,
            Version = 1,
            ProviderId = providerId,
            ClinicId = clinicId,
            AppointmentId = appointmentId,
            IsNoShow = isNoShow,
            Vitals = new PatientReportVitals(),
            WorkflowStatus = ReportWorkflowStatus.Draft,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateHeader(DateTime reportDate, Guid? providerId, Guid? clinicId, bool isNoShow)
    {
        EnsureNotSigned();
        ReportDate = reportDate;
        ProviderId = providerId;
        ClinicId = clinicId;
        IsNoShow = isNoShow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetVitals(PatientReportVitals vitals)
    {
        EnsureNotSigned();
        ArgumentNullException.ThrowIfNull(vitals);
        Vitals = vitals;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetFieldValues(IEnumerable<(int FieldId, string Text)> values)
    {
        EnsureNotSigned();
        ArgumentNullException.ThrowIfNull(values);
        _fieldValues.Clear();
        foreach ((int fieldId, string text) in values)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _fieldValues.Add(PatientReportFieldValue.Create(Id, fieldId, text));
            }
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Sign(string userId, string? name, string? signatureImagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        IsSigned = true;
        SignedByUserId = userId;
        SignedByName = name;
        SignedOnUtc = DateTime.UtcNow;
        SignatureImagePath = signatureImagePath;
        WorkflowStatus = ReportWorkflowStatus.Signed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Replace the set of problems associated with this report. Allowed after signing
    /// (associating problems is a review/curation action, not part of the locked report body).</summary>
    public void SetAssociatedProblems(IEnumerable<Guid> problemIds)
    {
        ArgumentNullException.ThrowIfNull(problemIds);
        _associatedProblems.Clear();
        foreach (Guid problemId in problemIds.Distinct())
        {
            if (problemId != Guid.Empty)
            {
                _associatedProblems.Add(PatientReportProblem.Create(Id, problemId));
            }
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public PatientReportAddendum AddAddendum(string userId, string? name, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        PatientReportAddendum addendum = PatientReportAddendum.Create(Id, text.Trim(), userId, name);
        _addendums.Add(addendum);
        UpdatedAtUtc = DateTime.UtcNow;
        return addendum;
    }

    public void RequestReview(string userId, Guid reviewerProviderId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ReviewRequestedByUserId = userId;
        ReviewRequestedOnUtc = DateTime.UtcNow;
        ReviewerProviderId = reviewerProviderId;
        WorkflowStatus = ReportWorkflowStatus.ReviewRequested;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReviewSign(string userId, string? name, string? signatureImagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ReviewSignedByUserId = userId;
        ReviewSignedByName = name;
        ReviewSignedOnUtc = DateTime.UtcNow;
        ReviewSignatureImagePath = signatureImagePath;
        WorkflowStatus = ReportWorkflowStatus.Reviewed;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureNotSigned()
    {
        if (IsSigned)
        {
            throw new InvalidOperationException("A signed report cannot be edited. Add an addendum instead.");
        }
    }
}
