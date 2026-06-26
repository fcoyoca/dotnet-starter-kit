using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;

public static class SetProviderSignatureEndpoint
{
    internal static RouteHandlerBuilder MapSetProviderSignatureEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/providers/{id:guid}/signature",
                async (Guid id, SetProviderSignatureCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetProviderSignatureCommand command = body with { ProviderId = id };
                    string url = await mediator.Send(command, ct);
                    return Results.Ok(url);
                })
            .WithName("SetProviderSignature")
            .WithSummary("Upload or replace a provider's signature image")
            .RequirePermission(AdministrationPermissions.Providers.Update);
    }
}
