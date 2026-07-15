using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkReady;

public static class MarkClaimReadyEndpoint
{
    internal static RouteHandlerBuilder MapMarkClaimReadyEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/ready",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new MarkClaimReadyCommand(claimId), ct)))
            .WithName("MarkClaimReady")
            .WithSummary("Mark a draft claim ready to submit")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
