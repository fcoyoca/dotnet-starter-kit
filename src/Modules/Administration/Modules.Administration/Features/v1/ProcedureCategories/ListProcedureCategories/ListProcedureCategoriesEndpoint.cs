using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.ListProcedureCategories;

public static class ListProcedureCategoriesEndpoint
{
    internal static RouteHandlerBuilder MapListProcedureCategoriesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/procedure-categories",
                async (
                    string? search,
                    bool? isActive,
                    bool? isImaging,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListProcedureCategoriesQuery(search, isActive, isImaging, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListProcedureCategories")
            .WithSummary("Search and list procedure categories")
            .RequirePermission(AdministrationPermissions.ProcedureCategories.View);
    }
}
