using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.CreateReferralType;

public static class CreateReferralTypeEndpoint
{
    internal static RouteHandlerBuilder MapCreateReferralTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/referral-types",
                async (CreateReferralTypeCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateReferralType")
            .WithSummary("Create a referral type")
            .RequirePermission(AdministrationPermissions.ReferralTypes.Create)
            .WithIdempotency();
    }
}
