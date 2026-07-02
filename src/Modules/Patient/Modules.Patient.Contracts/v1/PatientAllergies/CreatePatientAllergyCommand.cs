using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record CreatePatientAllergyCommand(
    Guid PatientId,
    string DrugName,
    string? RxAui,
    string? Reaction,
    string? Comments,
    DateTime DateNoted,
    bool IsActive = true) : ICommand<Guid>;
