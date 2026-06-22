using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Macros;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Macros.UpdateMacro;

public static class UpdateMacroEndpoint
{
    internal static RouteHandlerBuilder MapUpdateMacroEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/macros/{id:guid}",
                async (Guid id, UpdateMacroCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateMacroCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateMacro")
            .WithSummary("Update a macro")
            .RequirePermission(AdministrationPermissions.Macros.Update);
    }
}
