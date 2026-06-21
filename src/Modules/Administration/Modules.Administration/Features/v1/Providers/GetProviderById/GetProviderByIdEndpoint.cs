using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.GetProviderById;

public static class GetProviderByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetProviderByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/providers/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetProviderByIdQuery(id), ct))
            .WithName("GetProviderById")
            .WithSummary("Get a provider by id")
            .RequirePermission(AdministrationPermissions.Providers.View);
    }
}
