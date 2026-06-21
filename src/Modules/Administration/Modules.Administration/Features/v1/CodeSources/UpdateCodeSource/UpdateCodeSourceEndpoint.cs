using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CodeSources.UpdateCodeSource;

public static class UpdateCodeSourceEndpoint
{
    internal static RouteHandlerBuilder MapUpdateCodeSourceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/code-sources/{id:int}",
                async (int id, UpdateCodeSourceCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateCodeSourceCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateCodeSource")
            .WithSummary("Update a procedure code source")
            .RequirePermission(AdministrationPermissions.CodeSources.Update);
    }
}
