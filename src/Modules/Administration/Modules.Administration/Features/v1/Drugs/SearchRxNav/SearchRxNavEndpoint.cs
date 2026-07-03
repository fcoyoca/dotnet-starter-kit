using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.SearchRxNav;

public static class SearchRxNavEndpoint
{
    internal static RouteHandlerBuilder MapSearchRxNavEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/drugs/rxnav",
                async (string term, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new SearchRxNavQuery(term), ct)))
            .WithName("SearchRxNav")
            .WithSummary("Search the NIH RxNav API for drug concepts to import (admin-controlled sync)")
            .RequirePermission(AdministrationPermissions.Drugs.Create);
    }
}
