using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.GetEthnicityById;

public static class GetEthnicityByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetEthnicityByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/ethnicities/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetEthnicityByIdQuery(id), ct))
            .WithName("GetEthnicityById")
            .WithSummary("Get an ethnicity by id")
            .RequirePermission(AdministrationPermissions.Ethnicities.View);
    }
}
