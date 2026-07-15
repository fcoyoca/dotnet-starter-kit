using FSH.Modules.Claims.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record GetClaimsQuery(
    ClaimStatus? Status = null,
    Guid? InsuranceTypeId = null,
    string? Search = null,
    int PageNumber = 1,
    int PageSize = 20) : IQuery<ClaimsPageDto>;
