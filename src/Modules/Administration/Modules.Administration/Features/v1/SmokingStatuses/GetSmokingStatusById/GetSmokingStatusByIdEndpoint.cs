using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.GetSmokingStatusById;

public static class GetSmokingStatusByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetSmokingStatusByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/smoking-statuses/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetSmokingStatusByIdQuery(id), ct))
            .WithName("GetSmokingStatusById")
            .WithSummary("Get a smoking status by id")
            .RequirePermission(AdministrationPermissions.SmokingStatuses.View);
    }
}
