using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.UpdateDiagnostic;

public static class UpdateDiagnosticEndpoint
{
    internal static RouteHandlerBuilder MapUpdateDiagnosticEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/diagnostics/{id:int}",
                async (int id, UpdateDiagnosticCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateDiagnosticCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateDiagnostic")
            .WithSummary("Update an ICD diagnostic")
            .RequirePermission(AdministrationPermissions.Diagnostics.Update);
    }
}
