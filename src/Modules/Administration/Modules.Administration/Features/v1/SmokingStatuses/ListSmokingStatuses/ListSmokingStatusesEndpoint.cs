using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.ListSmokingStatuses;

public static class ListSmokingStatusesEndpoint
{
    internal static RouteHandlerBuilder MapListSmokingStatusesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/smoking-statuses",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListSmokingStatusesQuery(isActive), ct))
            .WithName("ListSmokingStatuses")
            .WithSummary("List all smoking statuses")
            .RequirePermission(AdministrationPermissions.SmokingStatuses.View);
    }
}
