using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReferralTypes;

public sealed record GetReferralTypeByIdQuery(int Id) : IQuery<LookupItemDto>;
