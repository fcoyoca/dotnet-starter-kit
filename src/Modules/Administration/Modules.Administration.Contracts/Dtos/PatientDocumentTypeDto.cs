namespace FSH.Modules.Administration.Contracts.Dtos;

public sealed record PatientDocumentTypeDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
