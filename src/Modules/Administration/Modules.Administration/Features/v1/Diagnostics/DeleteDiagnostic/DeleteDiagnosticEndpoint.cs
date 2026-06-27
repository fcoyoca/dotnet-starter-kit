using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.DeleteDiagnostic;

public static class DeleteDiagnosticEndpoint
{
    internal static RouteHandlerBuilder MapDeleteDiagnosticEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/diagnostics/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteDiagnosticCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteDiagnostic")
            .WithSummary("Soft-delete an ICD diagnostic")
            .RequirePermission(AdministrationPermissions.Diagnostics.Delete);
    }
}
