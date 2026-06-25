namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientIncidentDetailDto(
    Guid Id,
    Guid PatientId,
    Guid? IncidentTypeId,
    Guid? DepartmentId,
    DateTime? DateOfInitialVisit,
    DateTime DateOfLoss,
    bool IsClosed,
    bool IsTransfer,
    bool IsAccident,
    AccidentType? AccidentType,
    string? AccidentState,
    IncidentPatientStatus PatientStatus,
    IReadOnlyList<Guid> DiagnosticIds,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    string? Comments,
    string? SummaryOfCare,
    int? AdherenceToPlan);
