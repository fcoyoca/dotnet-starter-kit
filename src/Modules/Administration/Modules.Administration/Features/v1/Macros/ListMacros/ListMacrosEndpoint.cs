using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Macros;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Macros.ListMacros;

public static class ListMacrosEndpoint
{
    internal static RouteHandlerBuilder MapListMacrosEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/macros",
                async (
                    string? search,
                    bool? isActive,
                    int? reportFieldId,
                    bool? general,
                    Guid? useableByUserId,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    string? reportFieldName,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListMacrosQuery(search, isActive, reportFieldId, general, useableByUserId, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir, reportFieldName), ct)))
            .WithName("ListMacros")
            .WithSummary("Search and list macros")
            .RequirePermission(AdministrationPermissions.Macros.View);
    }
}
