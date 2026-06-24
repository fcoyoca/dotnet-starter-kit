using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Clinics;

public sealed record CreateClinicCommand(
    string Code,
    string Name,
    string Address1,
    string? Address2,
    string City,
    string State,
    string Zip,
    string? Phone,
    string? TimeZoneId = null) : ICommand<Guid>;
