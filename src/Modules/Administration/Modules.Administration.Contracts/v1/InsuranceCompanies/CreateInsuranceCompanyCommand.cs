using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;

public sealed record CreateInsuranceCompanyCommand(
    string Name,
    Guid? InsuranceTypeId = null,
    int FormularyTiers = 0,
    string? Address1 = null,
    string? Address2 = null,
    string? City = null,
    string? State = null,
    string? Zip = null,
    string? Phone = null) : ICommand<Guid>;
