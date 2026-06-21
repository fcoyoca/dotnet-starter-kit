using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.UpdateProvider;

public static class UpdateProviderEndpoint
{
    internal static RouteHandlerBuilder MapUpdateProviderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/providers/{id:guid}",
                async (Guid id, UpdateProviderCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateProviderCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateProvider")
            .WithSummary("Update a provider")
            .RequirePermission(AdministrationPermissions.Providers.Update);
    }
}
