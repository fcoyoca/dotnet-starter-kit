using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

public sealed record GetInsuranceTypeByIdQuery(Guid Id) : IQuery<InsuranceTypeDto>;
