using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkDenied;

public static class MarkClaimDeniedEndpoint
{
    internal static RouteHandlerBuilder MapMarkClaimDeniedEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/denied",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new MarkClaimDeniedCommand(claimId), ct)))
            .WithName("MarkClaimDenied")
            .WithSummary("Mark a submitted claim denied")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
