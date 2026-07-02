using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.ListDrugs;

public static class ListDrugsEndpoint
{
    internal static RouteHandlerBuilder MapListDrugsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/drugs",
                async (
                    string? search,
                    bool? isActive,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListDrugsQuery(search, isActive, pageNumber ?? 1, pageSize ?? 20), ct)))
            .WithName("ListDrugs")
            .WithSummary("Search and list the drug catalog")
            .RequirePermission(AdministrationPermissions.Drugs.View);
    }
}
