using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.DeleteProvider;

public static class DeleteProviderEndpoint
{
    internal static RouteHandlerBuilder MapDeleteProviderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/providers/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteProviderCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteProvider")
            .WithSummary("Delete a provider")
            .RequirePermission(AdministrationPermissions.Providers.Delete);
    }
}
