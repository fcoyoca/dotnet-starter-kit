using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.DeleteDiagnosticCategory;

public static class DeleteDiagnosticCategoryEndpoint
{
    internal static RouteHandlerBuilder MapDeleteDiagnosticCategoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/diagnostic-categories/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteDiagnosticCategoryCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteDiagnosticCategory")
            .WithSummary("Delete a diagnostic category")
            .RequirePermission(AdministrationPermissions.DiagnosticCategories.Delete);
    }
}
