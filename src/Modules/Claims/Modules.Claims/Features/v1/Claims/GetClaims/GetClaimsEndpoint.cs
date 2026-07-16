using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ContractsClaimStatus = FSH.Modules.Claims.Contracts.ClaimStatus;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaims;

public static class GetClaimsEndpoint
{
    internal static RouteHandlerBuilder MapGetClaimsEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/",
                (ContractsClaimStatus? status, Guid? insuranceTypeId, string? search,
                 int pageNumber, int pageSize, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetClaimsQuery(
                        status, insuranceTypeId, search,
                        pageNumber <= 0 ? 1 : pageNumber,
                        pageSize <= 0 ? 20 : Math.Min(pageSize, 100)), ct))
            .WithName("GetClaims")
            .WithSummary("List claims (worklist) with status summary")
            .RequirePermission(ClaimsPermissions.View);
}
