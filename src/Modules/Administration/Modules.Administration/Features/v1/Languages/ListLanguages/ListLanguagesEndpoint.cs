using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Languages;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Languages.ListLanguages;

public static class ListLanguagesEndpoint
{
    internal static RouteHandlerBuilder MapListLanguagesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/languages",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListLanguagesQuery(isActive), ct))
            .WithName("ListLanguages")
            .WithSummary("List all languages")
            .RequirePermission(AdministrationPermissions.Languages.View);
    }
}
