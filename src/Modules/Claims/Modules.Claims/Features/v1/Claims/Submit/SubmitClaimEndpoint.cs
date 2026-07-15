using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.Submit;

public static class SubmitClaimEndpoint
{
    internal static RouteHandlerBuilder MapSubmitClaimEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/submit",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new SubmitClaimCommand(claimId), ct)))
            .WithName("SubmitClaim")
            .WithSummary("Submit a ready claim to the clearinghouse (stub)")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
