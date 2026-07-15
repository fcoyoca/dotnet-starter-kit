using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.Void;

public static class VoidClaimEndpoint
{
    internal static RouteHandlerBuilder MapVoidClaimEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/void",
                async (Guid claimId, VoidClaimRequest? body, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new VoidClaimCommand(claimId, body?.Reason), ct)))
            .WithName("VoidClaim")
            .WithSummary("Void a non-terminal claim")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}

public sealed record VoidClaimRequest(string? Reason);
