using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Races;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Races.CreateRace;

public static class CreateRaceEndpoint
{
    internal static RouteHandlerBuilder MapCreateRaceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/races",
                async (CreateRaceCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateRace")
            .WithSummary("Create a race")
            .RequirePermission(AdministrationPermissions.Races.Create)
            .WithIdempotency();
    }
}
