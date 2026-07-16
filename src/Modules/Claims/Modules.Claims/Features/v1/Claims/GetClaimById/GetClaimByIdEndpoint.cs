using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.GetClaimById;

public static class GetClaimByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetClaimByIdEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/{claimId:guid}",
                (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetClaimByIdQuery(claimId), ct))
            .WithName("GetClaimById")
            .WithSummary("Get a claim with its snapshot lines")
            .RequirePermission(ClaimsPermissions.View);
}
