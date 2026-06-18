using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Races;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Races.ListRaces;

public static class ListRacesEndpoint
{
    internal static RouteHandlerBuilder MapListRacesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/races",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListRacesQuery(isActive), ct))
            .WithName("ListRaces")
            .WithSummary("List all races")
            .RequirePermission(AdministrationPermissions.Races.View);
    }
}
