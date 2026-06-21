using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CodeSources.DeleteCodeSource;

public static class DeleteCodeSourceEndpoint
{
    internal static RouteHandlerBuilder MapDeleteCodeSourceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/code-sources/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteCodeSourceCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteCodeSource")
            .WithSummary("Delete a procedure code source")
            .RequirePermission(AdministrationPermissions.CodeSources.Delete);
    }
}
