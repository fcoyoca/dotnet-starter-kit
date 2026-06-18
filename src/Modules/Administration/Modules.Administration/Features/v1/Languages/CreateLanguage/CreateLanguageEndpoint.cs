using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Languages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Languages.CreateLanguage;

public static class CreateLanguageEndpoint
{
    internal static RouteHandlerBuilder MapCreateLanguageEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/languages",
                async (CreateLanguageCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateLanguage")
            .WithSummary("Create a language")
            .RequirePermission(AdministrationPermissions.Languages.Create)
            .WithIdempotency();
    }
}
