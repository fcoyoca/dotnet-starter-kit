using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.CreateProvider;

public static class CreateProviderEndpoint
{
    internal static RouteHandlerBuilder MapCreateProviderEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/providers",
                async (CreateProviderCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateProvider")
            .WithSummary("Create a provider")
            .RequirePermission(AdministrationPermissions.Providers.Create)
            .WithIdempotency();
    }
}
