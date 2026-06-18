using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.ListEthnicities;

public static class ListEthnicitiesEndpoint
{
    internal static RouteHandlerBuilder MapListEthnicitiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/ethnicities",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListEthnicitiesQuery(isActive), ct))
            .WithName("ListEthnicities")
            .WithSummary("List all ethnicities")
            .RequirePermission(AdministrationPermissions.Ethnicities.View);
    }
}
