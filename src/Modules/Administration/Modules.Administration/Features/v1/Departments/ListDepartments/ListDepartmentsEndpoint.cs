using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Departments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Departments.ListDepartments;

public static class ListDepartmentsEndpoint
{
    internal static RouteHandlerBuilder MapListDepartmentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/departments",
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
                        new ListDepartmentsQuery(search, isActive, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListDepartments")
            .WithSummary("Search and list departments")
            .RequirePermission(AdministrationPermissions.Departments.View);
    }
}
