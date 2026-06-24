using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Clinics;

public sealed record UpdateClinicCommand(
    Guid Id,
    string Code,
    string Name,
    string Address1,
    string? Address2,
    string City,
    string State,
    string Zip,
    string? Phone,
    bool IsActive,
    string? TimeZoneId = null) : ICommand<Unit>;
