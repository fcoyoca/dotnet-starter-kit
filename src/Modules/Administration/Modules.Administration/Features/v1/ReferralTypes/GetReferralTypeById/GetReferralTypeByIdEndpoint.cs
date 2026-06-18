using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.GetReferralTypeById;

public static class GetReferralTypeByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetReferralTypeByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/referral-types/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetReferralTypeByIdQuery(id), ct))
            .WithName("GetReferralTypeById")
            .WithSummary("Get a referral type by id")
            .RequirePermission(AdministrationPermissions.ReferralTypes.View);
    }
}
