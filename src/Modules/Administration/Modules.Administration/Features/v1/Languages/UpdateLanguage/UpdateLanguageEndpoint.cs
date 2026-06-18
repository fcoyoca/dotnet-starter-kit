using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Languages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Languages.UpdateLanguage;

public static class UpdateLanguageEndpoint
{
    internal static RouteHandlerBuilder MapUpdateLanguageEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/languages/{id:int}",
                async (int id, UpdateLanguageCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateLanguageCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateLanguage")
            .WithSummary("Update a language")
            .RequirePermission(AdministrationPermissions.Languages.Update);
    }
}
