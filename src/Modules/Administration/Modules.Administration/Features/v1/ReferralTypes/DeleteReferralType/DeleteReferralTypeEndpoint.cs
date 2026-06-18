using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.DeleteReferralType;

public static class DeleteReferralTypeEndpoint
{
    internal static RouteHandlerBuilder MapDeleteReferralTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/referral-types/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteReferralTypeCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteReferralType")
            .WithSummary("Delete a referral type")
            .RequirePermission(AdministrationPermissions.ReferralTypes.Delete);
    }
}
