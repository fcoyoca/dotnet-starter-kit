using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Races;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Races.DeleteRace;

public static class DeleteRaceEndpoint
{
    internal static RouteHandlerBuilder MapDeleteRaceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/races/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteRaceCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteRace")
            .WithSummary("Delete a race")
            .RequirePermission(AdministrationPermissions.Races.Delete);
    }
}
