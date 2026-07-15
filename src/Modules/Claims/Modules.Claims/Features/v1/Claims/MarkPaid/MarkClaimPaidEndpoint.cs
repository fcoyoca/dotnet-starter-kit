using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Claims.Contracts.Authorization;
using FSH.Modules.Claims.Contracts.v1.Claims;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Claims.Features.v1.Claims.MarkPaid;

public static class MarkClaimPaidEndpoint
{
    internal static RouteHandlerBuilder MapMarkClaimPaidEndpoint(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/{claimId:guid}/paid",
                async (Guid claimId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new MarkClaimPaidCommand(claimId), ct)))
            .WithName("MarkClaimPaid")
            .WithSummary("Mark a submitted claim paid")
            .RequirePermission(ClaimsPermissions.Manage)
            .WithIdempotency();
}
