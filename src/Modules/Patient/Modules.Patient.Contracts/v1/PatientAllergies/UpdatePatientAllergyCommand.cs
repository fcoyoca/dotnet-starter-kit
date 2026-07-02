using Mediator;

namespace FSH.Modules.Patient.Contracts.v1.PatientAllergies;

public sealed record UpdatePatientAllergyCommand(
    Guid AllergyId,
    string DrugName,
    string? RxAui,
    string? Reaction,
    string? Comments,
    DateTime DateNoted,
    bool IsActive) : ICommand<Unit>;
