using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.DeleteCustomDiagnostic;

public static class DeleteCustomDiagnosticEndpoint
{
    internal static RouteHandlerBuilder MapDeleteCustomDiagnosticEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/custom-diagnostics/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteCustomDiagnosticCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteCustomDiagnostic")
            .WithSummary("Delete a custom diagnostic")
            .RequirePermission(AdministrationPermissions.CustomDiagnostics.Delete);
    }
}
