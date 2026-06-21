using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CodeSources.CreateCodeSource;

public static class CreateCodeSourceEndpoint
{
    internal static RouteHandlerBuilder MapCreateCodeSourceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/code-sources",
                async (CreateCodeSourceCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateCodeSource")
            .WithSummary("Create a procedure code source")
            .RequirePermission(AdministrationPermissions.CodeSources.Create)
            .WithIdempotency();
    }
}
