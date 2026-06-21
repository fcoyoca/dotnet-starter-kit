using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.UpdateDiagnosticCategory;

public static class UpdateDiagnosticCategoryEndpoint
{
    internal static RouteHandlerBuilder MapUpdateDiagnosticCategoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/diagnostic-categories/{id:guid}",
                async (Guid id, UpdateDiagnosticCategoryCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateDiagnosticCategoryCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateDiagnosticCategory")
            .WithSummary("Update a diagnostic category")
            .RequirePermission(AdministrationPermissions.DiagnosticCategories.Update);
    }
}
