using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientMedications;

public sealed record UpdatePatientMedicationCommand(
    Guid MedicationId,
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
    bool IsActive) : ICommand<Unit>;
