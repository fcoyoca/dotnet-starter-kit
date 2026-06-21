using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Departments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Departments.DeleteDepartment;

public static class DeleteDepartmentEndpoint
{
    internal static RouteHandlerBuilder MapDeleteDepartmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/departments/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteDepartmentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteDepartment")
            .WithSummary("Delete a department")
            .RequirePermission(AdministrationPermissions.Departments.Delete);
    }
}
