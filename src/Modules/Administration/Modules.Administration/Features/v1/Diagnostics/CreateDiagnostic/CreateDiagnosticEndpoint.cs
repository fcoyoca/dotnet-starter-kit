using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.CreateDiagnostic;

public static class CreateDiagnosticEndpoint
{
    internal static RouteHandlerBuilder MapCreateDiagnosticEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/diagnostics",
                async (CreateDiagnosticCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateDiagnostic")
            .WithSummary("Create an ICD diagnostic")
            .RequirePermission(AdministrationPermissions.Diagnostics.Create)
            .WithIdempotency();
    }
}
