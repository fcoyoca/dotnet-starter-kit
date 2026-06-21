using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.ListInsuranceTypes;

public static class ListInsuranceTypesEndpoint
{
    internal static RouteHandlerBuilder MapListInsuranceTypesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/insurance-types",
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
                        new ListInsuranceTypesQuery(search, isActive, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListInsuranceTypes")
            .WithSummary("Search and list insurance types")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.View);
    }
}
