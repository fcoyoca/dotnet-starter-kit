using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Departments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Departments.UpdateDepartment;

public static class UpdateDepartmentEndpoint
{
    internal static RouteHandlerBuilder MapUpdateDepartmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/departments/{id:guid}",
                async (Guid id, UpdateDepartmentCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateDepartmentCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateDepartment")
            .WithSummary("Update a department")
            .RequirePermission(AdministrationPermissions.Departments.Update);
    }
}
