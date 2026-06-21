using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Departments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Departments.GetDepartmentById;

public static class GetDepartmentByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetDepartmentByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/departments/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetDepartmentByIdQuery(id), ct))
            .WithName("GetDepartmentById")
            .WithSummary("Get a department by id")
            .RequirePermission(AdministrationPermissions.Departments.View);
    }
}
