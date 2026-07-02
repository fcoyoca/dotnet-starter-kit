namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientNoteDto(
    Guid Id,
    Guid PatientId,
    string Name,
    string? Description,
    bool IsMedicalAlert,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
