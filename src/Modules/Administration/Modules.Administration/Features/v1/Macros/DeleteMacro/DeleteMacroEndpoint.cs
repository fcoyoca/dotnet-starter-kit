using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Macros;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Macros.DeleteMacro;

public static class DeleteMacroEndpoint
{
    internal static RouteHandlerBuilder MapDeleteMacroEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/macros/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteMacroCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteMacro")
            .WithSummary("Delete a macro")
            .RequirePermission(AdministrationPermissions.Macros.Delete);
    }
}
