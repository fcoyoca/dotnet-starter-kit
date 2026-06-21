using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;

public sealed record UpdateInsuranceCompanyCommand(
    Guid Id,
    string Name,
    Guid? InsuranceTypeId,
    int FormularyTiers,
    string? Address1,
    string? Address2,
    string? City,
    string? State,
    string? Zip,
    string? Phone,
    bool IsActive) : ICommand<Unit>;
