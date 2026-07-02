namespace FSH.Modules.Patient.Contracts.Dtos;

public sealed record PatientMedicationDto(
    Guid Id,
    Guid PatientId,
    string DrugName,
    string? RxAui,
    string? RxCode,
    string? Ndc,
    string? Prescriber,
    DateTime StartDate,
    DateTime? EndDate,
    decimal? DoseValue,
    int? DoseUnitId,
    decimal? DosePeriodValue,
    string? DosePeriodUnit,
    string? Instructions,
    string? Indication,
    bool IsActive,
    string? CreatedByName,
    DateTime CreatedAtUtc,
    string? UpdatedByName,
    DateTime? UpdatedAtUtc);
