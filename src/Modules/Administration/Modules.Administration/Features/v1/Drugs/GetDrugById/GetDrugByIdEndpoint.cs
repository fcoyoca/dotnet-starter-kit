using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.GetDrugById;

public static class GetDrugByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetDrugByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/drugs/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetDrugByIdQuery(id), ct))
            .WithName("GetDrugById")
            .WithSummary("Get a drug catalog entry by id")
            .RequirePermission(AdministrationPermissions.Drugs.View);
    }
}
