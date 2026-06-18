using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Languages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Languages.DeleteLanguage;

public static class DeleteLanguageEndpoint
{
    internal static RouteHandlerBuilder MapDeleteLanguageEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/languages/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteLanguageCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteLanguage")
            .WithSummary("Delete a language")
            .RequirePermission(AdministrationPermissions.Languages.Delete);
    }
}
