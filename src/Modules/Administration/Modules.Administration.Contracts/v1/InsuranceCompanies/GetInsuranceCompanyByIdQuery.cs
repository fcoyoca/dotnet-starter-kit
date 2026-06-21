using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;

public sealed record GetInsuranceCompanyByIdQuery(Guid Id) : IQuery<InsuranceCompanyDto>;
