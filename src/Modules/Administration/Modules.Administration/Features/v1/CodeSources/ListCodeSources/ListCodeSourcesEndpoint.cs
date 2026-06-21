using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CodeSources.ListCodeSources;

public static class ListCodeSourcesEndpoint
{
    internal static RouteHandlerBuilder MapListCodeSourcesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/code-sources",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListCodeSourcesQuery(isActive), ct))
            .WithName("ListCodeSources")
            .WithSummary("List all procedure code sources")
            .RequirePermission(AdministrationPermissions.CodeSources.View);
    }
}
