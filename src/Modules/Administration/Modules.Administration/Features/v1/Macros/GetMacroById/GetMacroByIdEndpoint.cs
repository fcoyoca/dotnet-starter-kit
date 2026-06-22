using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Macros;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Macros.GetMacroById;

public static class GetMacroByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetMacroByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/macros/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetMacroByIdQuery(id), ct))
            .WithName("GetMacroById")
            .WithSummary("Get a macro by id")
            .RequirePermission(AdministrationPermissions.Macros.View);
    }
}
