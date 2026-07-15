namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientReportListItemDto(
    Guid Id,
    Guid IncidentId,
    Guid PatientId,
    int ReportTypeId,
    DateTime ReportDate,
    int Version,
    Guid? ProviderId,
    Guid? ClinicId,
    bool IsNoShow,
    ReportWorkflowStatus WorkflowStatus,
    bool IsSigned,
    string? SignedByName,
    DateTime? SignedOnUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    bool IsDeleted = false);
