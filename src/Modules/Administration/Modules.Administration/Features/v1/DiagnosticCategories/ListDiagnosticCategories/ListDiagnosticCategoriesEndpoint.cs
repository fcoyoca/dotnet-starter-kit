using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.ListDiagnosticCategories;

public static class ListDiagnosticCategoriesEndpoint
{
    internal static RouteHandlerBuilder MapListDiagnosticCategoriesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/diagnostic-categories",
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
                        new ListDiagnosticCategoriesQuery(search, isActive, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListDiagnosticCategories")
            .WithSummary("Search and list diagnostic categories")
            .RequirePermission(AdministrationPermissions.DiagnosticCategories.View);
    }
}
