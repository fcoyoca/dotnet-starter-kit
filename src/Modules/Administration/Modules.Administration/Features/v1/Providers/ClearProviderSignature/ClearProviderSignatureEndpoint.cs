using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;

public static class ClearProviderSignatureEndpoint
{
    internal static RouteHandlerBuilder MapClearProviderSignatureEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/providers/{id:guid}/signature",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ClearProviderSignatureCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("ClearProviderSignature")
            .WithSummary("Remove a provider's signature image")
            .RequirePermission(AdministrationPermissions.Providers.Update);
    }
}
