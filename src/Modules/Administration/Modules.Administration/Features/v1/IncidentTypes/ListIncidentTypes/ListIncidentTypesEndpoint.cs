using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.ListIncidentTypes;

public static class ListIncidentTypesEndpoint
{
    internal static RouteHandlerBuilder MapListIncidentTypesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/incident-types",
                async (
                    string? search,
                    bool? isActive,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListIncidentTypesQuery(search, isActive, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListIncidentTypes")
            .WithSummary("Search and list incident types")
            .RequirePermission(AdministrationPermissions.IncidentTypes.View);
    }
}
