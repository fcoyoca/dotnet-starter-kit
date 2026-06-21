using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

public sealed record CreateProviderCommand(
    string FirstName,
    string LastName,
    string? Prefix = null,
    string? Suffix = null,
    string? Specialty = null,
    string? Npi = null,
    string? KareoExternalId = null,
    Guid? PrimaryClinicId = null,
    string? UserId = null) : ICommand<Guid>;
