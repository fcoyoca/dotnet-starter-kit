using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

public sealed record UpdateProviderCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Prefix,
    string? Suffix,
    string? Specialty,
    string? Npi,
    string? KareoExternalId,
    Guid? PrimaryClinicId,
    string? UserId,
    bool IsActive) : ICommand<Unit>;
