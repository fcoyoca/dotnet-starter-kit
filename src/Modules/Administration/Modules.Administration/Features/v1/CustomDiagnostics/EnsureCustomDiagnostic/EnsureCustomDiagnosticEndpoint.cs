using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.EnsureCustomDiagnostic;

public static class EnsureCustomDiagnosticEndpoint
{
    internal static RouteHandlerBuilder MapEnsureCustomDiagnosticEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/custom-diagnostics/ensure",
                async (EnsureCustomDiagnosticCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("EnsureCustomDiagnostic")
            .WithSummary("Find or create a tenant custom diagnostic from a picked global code")
            .RequirePermission(AdministrationPermissions.CustomDiagnostics.Create)
            .WithIdempotency();
    }
}
