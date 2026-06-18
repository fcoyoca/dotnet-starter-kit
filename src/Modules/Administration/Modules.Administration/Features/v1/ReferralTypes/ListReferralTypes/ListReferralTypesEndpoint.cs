using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.ListReferralTypes;

public static class ListReferralTypesEndpoint
{
    internal static RouteHandlerBuilder MapListReferralTypesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/referral-types",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListReferralTypesQuery(isActive), ct))
            .WithName("ListReferralTypes")
            .WithSummary("List all referral types")
            .RequirePermission(AdministrationPermissions.ReferralTypes.View);
    }
}
