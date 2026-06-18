using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Races;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Races.UpdateRace;

public static class UpdateRaceEndpoint
{
    internal static RouteHandlerBuilder MapUpdateRaceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/races/{id:int}",
                async (int id, UpdateRaceCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateRaceCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateRace")
            .WithSummary("Update a race")
            .RequirePermission(AdministrationPermissions.Races.Update);
    }
}
