using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Macros;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Macros.CreateMacro;

public static class CreateMacroEndpoint
{
    internal static RouteHandlerBuilder MapCreateMacroEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/macros",
                async (CreateMacroCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateMacro")
            .WithSummary("Create a macro")
            .RequirePermission(AdministrationPermissions.Macros.Create)
            .WithIdempotency();
    }
}
