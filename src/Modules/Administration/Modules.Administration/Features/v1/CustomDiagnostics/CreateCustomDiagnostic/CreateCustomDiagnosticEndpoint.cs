using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.CreateCustomDiagnostic;

public static class CreateCustomDiagnosticEndpoint
{
    internal static RouteHandlerBuilder MapCreateCustomDiagnosticEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/custom-diagnostics",
                async (CreateCustomDiagnosticCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateCustomDiagnostic")
            .WithSummary("Create a custom diagnostic")
            .RequirePermission(AdministrationPermissions.CustomDiagnostics.Create)
            .WithIdempotency();
    }
}
