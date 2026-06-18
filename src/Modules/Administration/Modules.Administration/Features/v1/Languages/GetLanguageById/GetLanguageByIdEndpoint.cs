using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Languages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Languages.GetLanguageById;

public static class GetLanguageByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetLanguageByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/languages/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetLanguageByIdQuery(id), ct))
            .WithName("GetLanguageById")
            .WithSummary("Get a language by id")
            .RequirePermission(AdministrationPermissions.Languages.View);
    }
}
