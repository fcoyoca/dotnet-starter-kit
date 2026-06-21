using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.UpdateProcedureCategory;

public static class UpdateProcedureCategoryEndpoint
{
    internal static RouteHandlerBuilder MapUpdateProcedureCategoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/procedure-categories/{id:guid}",
                async (Guid id, UpdateProcedureCategoryCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateProcedureCategoryCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateProcedureCategory")
            .WithSummary("Update a procedure category")
            .RequirePermission(AdministrationPermissions.ProcedureCategories.Update);
    }
}
