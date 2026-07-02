namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record MedicationReconciledDateDto(
    Guid Id,
    Guid PatientId,
    DateTime ReconciledOn,
    string? CreatedByName,
    DateTime CreatedAtUtc);
