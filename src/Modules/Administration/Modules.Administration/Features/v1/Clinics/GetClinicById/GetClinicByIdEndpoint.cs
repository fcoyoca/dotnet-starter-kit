using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Clinics.GetClinicById;

public static class GetClinicByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetClinicByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/clinics/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetClinicByIdQuery(id), ct))
            .WithName("GetClinicById")
            .WithSummary("Get a clinic by id")
            .RequirePermission(AdministrationPermissions.Clinics.View);
    }
}
