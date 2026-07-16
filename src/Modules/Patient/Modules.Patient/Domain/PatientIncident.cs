using FSH.Framework.Core.Domain;
using FSH.Modules.Patient.Contracts.Dtos;

namespace FSH.Modules.Patient.Domain;

public sealed class PatientIncident : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid PatientId { get; private set; }
    public Guid? IncidentTypeId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public DateTime? DateOfInitialVisit { get; private set; }
    public DateTime DateOfLoss { get; private set; }
    public bool IsClosed { get; private set; }
    public bool IsTransfer { get; private set; }
    public bool IsAccident { get; private set; }
    public AccidentType? AccidentType { get; private set; }
    public string? AccidentState { get; private set; }
    public string? Comments { get; private set; }
    public string? SummaryOfCare { get; private set; }
    public int? AdherenceToPlan { get; private set; }
    public IncidentPatientStatus PatientStatus { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public IReadOnlyList<PatientIncidentDiagnostic> Diagnostics => _diagnostics.AsReadOnly();
    private readonly List<PatientIncidentDiagnostic> _diagnostics = [];

    private PatientIncident() { }

    public static PatientIncident Create(
        Guid patientId,
        Guid? incidentTypeId,
        Guid? departmentId,
        DateTime? dateOfInitialVisit,
        DateTime dateOfLoss,
        bool isTransfer,
        bool isAccident,
        AccidentType? accidentType,
        string? accidentState,
        string? comments)
    {
        return new PatientIncident
        {
            Id = Guid.CreateVersion7(),
            PatientId = patientId,
            IncidentTypeId = incidentTypeId,
            DepartmentId = departmentId,
            DateOfInitialVisit = dateOfInitialVisit,
            DateOfLoss = dateOfLoss,
            IsTransfer = isTransfer,
            IsAccident = isAccident,
            AccidentType = isAccident ? accidentType : null,
            AccidentState = isAccident ? Trim(accidentState) : null,
            Comments = Trim(comments),
            PatientStatus = IncidentPatientStatus.Active,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(
        Guid? incidentTypeId,
        Guid? departmentId,
        DateTime dateOfInitialVisit,
        DateTime dateOfLoss,
        bool isTransfer,
        bool isAccident,
        AccidentType? accidentType,
        string? accidentState,
        string? comments,
        string? summaryOfCare,
        int? adherenceToPlan,
        IncidentPatientStatus patientStatus,
        bool isClosed,
        IList<Guid> diagnosticIds)
    {
        IncidentTypeId = incidentTypeId;
        DepartmentId = departmentId;
        DateOfInitialVisit = dateOfInitialVisit;
        DateOfLoss = dateOfLoss;
        IsTransfer = isTransfer;
        IsAccident = isAccident;
        AccidentType = isAccident ? accidentType : null;
        AccidentState = isAccident ? Trim(accidentState) : null;
        Comments = Trim(comments);
        SummaryOfCare = Trim(summaryOfCare);
        AdherenceToPlan = adherenceToPlan;
        PatientStatus = patientStatus;
        IsClosed = isClosed;
        SetDiagnostics(diagnosticIds);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Close()
    {
        IsClosed = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Delete(string? deletedBy)
    {
        IsDeleted = true;
        DeletedOnUtc = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void SetDiagnostics(IList<Guid> ids)
    {
        // Reconcile in place (see PatientReport.SetFieldValues) — Clear()+re-add on a tracked
        // aggregate triggers phantom writes → DbUpdateConcurrencyException on update.
        HashSet<Guid> desired = ids.ToHashSet();

        _diagnostics.RemoveAll(d => !desired.Contains(d.DiagnosticId));

        HashSet<Guid> present = _diagnostics.Select(d => d.DiagnosticId).ToHashSet();
        foreach (Guid id in desired.Where(id => !present.Contains(id)))
        {
            _diagnostics.Add(PatientIncidentDiagnostic.Create(Id, id));
        }
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
