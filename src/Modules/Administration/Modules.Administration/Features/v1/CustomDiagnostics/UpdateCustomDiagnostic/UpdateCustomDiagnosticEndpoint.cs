using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.UpdateCustomDiagnostic;

public static class UpdateCustomDiagnosticEndpoint
{
    internal static RouteHandlerBuilder MapUpdateCustomDiagnosticEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/custom-diagnostics/{id:guid}",
                async (Guid id, UpdateCustomDiagnosticCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateCustomDiagnosticCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateCustomDiagnostic")
            .WithSummary("Update a custom diagnostic")
            .RequirePermission(AdministrationPermissions.CustomDiagnostics.Update);
    }
}
