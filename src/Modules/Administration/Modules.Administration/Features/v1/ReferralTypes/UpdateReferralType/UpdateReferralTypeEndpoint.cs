using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.UpdateReferralType;

public static class UpdateReferralTypeEndpoint
{
    internal static RouteHandlerBuilder MapUpdateReferralTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/referral-types/{id:int}",
                async (int id, UpdateReferralTypeCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateReferralTypeCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateReferralType")
            .WithSummary("Update a referral type")
            .RequirePermission(AdministrationPermissions.ReferralTypes.Update);
    }
}
