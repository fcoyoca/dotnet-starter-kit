using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.DeleteProcedureCategory;

public static class DeleteProcedureCategoryEndpoint
{
    internal static RouteHandlerBuilder MapDeleteProcedureCategoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/procedure-categories/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteProcedureCategoryCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteProcedureCategory")
            .WithSummary("Delete a procedure category")
            .RequirePermission(AdministrationPermissions.ProcedureCategories.Delete);
    }
}
