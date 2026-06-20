using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Clinics.ListClinics;

public static class ListClinicsEndpoint
{
    internal static RouteHandlerBuilder MapListClinicsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/clinics",
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
                        new ListClinicsQuery(search, isActive, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListClinics")
            .WithSummary("Search and list clinics")
            .RequirePermission(AdministrationPermissions.Clinics.View);
    }
}
