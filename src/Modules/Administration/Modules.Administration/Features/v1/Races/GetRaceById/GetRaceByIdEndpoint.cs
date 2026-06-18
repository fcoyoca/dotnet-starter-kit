using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Races;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Races.GetRaceById;

public static class GetRaceByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetRaceByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/races/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetRaceByIdQuery(id), ct))
            .WithName("GetRaceById")
            .WithSummary("Get a race by id")
            .RequirePermission(AdministrationPermissions.Races.View);
    }
}
