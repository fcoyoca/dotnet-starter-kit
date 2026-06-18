using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.DeleteEthnicity;

public static class DeleteEthnicityEndpoint
{
    internal static RouteHandlerBuilder MapDeleteEthnicityEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/ethnicities/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteEthnicityCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteEthnicity")
            .WithSummary("Delete an ethnicity")
            .RequirePermission(AdministrationPermissions.Ethnicities.Delete);
    }
}
