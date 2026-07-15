using FSH.Modules.Claims.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record GetClaimByIdQuery(Guid ClaimId) : IQuery<ClaimDetailDto>;
