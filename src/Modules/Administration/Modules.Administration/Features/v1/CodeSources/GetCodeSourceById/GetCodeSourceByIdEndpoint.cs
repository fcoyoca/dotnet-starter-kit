using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CodeSources;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CodeSources.GetCodeSourceById;

public static class GetCodeSourceByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetCodeSourceByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/code-sources/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetCodeSourceByIdQuery(id), ct))
            .WithName("GetCodeSourceById")
            .WithSummary("Get a procedure code source by id")
            .RequirePermission(AdministrationPermissions.CodeSources.View);
    }
}
