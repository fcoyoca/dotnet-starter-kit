using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Departments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Departments.CreateDepartment;

public static class CreateDepartmentEndpoint
{
    internal static RouteHandlerBuilder MapCreateDepartmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/departments",
                async (CreateDepartmentCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateDepartment")
            .WithSummary("Create a department")
            .RequirePermission(AdministrationPermissions.Departments.Create)
            .WithIdempotency();
    }
}
