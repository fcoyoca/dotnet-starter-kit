using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.SetCodes;

public static class SetDiagnosticCategoryCodesEndpoint
{
    internal static RouteHandlerBuilder MapSetDiagnosticCategoryCodesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/diagnostic-categories/{id:guid}/codes",
                async (Guid id, SetDiagnosticCategoryCodesCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetDiagnosticCategoryCodesCommand command = body with { CategoryId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetDiagnosticCategoryCodes")
            .WithSummary("Replace the diagnostic-code associations for a diagnostic category")
            .RequirePermission(AdministrationPermissions.DiagnosticCategories.Update);
    }
}
