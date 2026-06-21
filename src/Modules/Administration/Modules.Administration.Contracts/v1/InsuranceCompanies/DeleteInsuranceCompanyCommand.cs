using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;

public sealed record DeleteInsuranceCompanyCommand(Guid Id) : ICommand<Unit>;
