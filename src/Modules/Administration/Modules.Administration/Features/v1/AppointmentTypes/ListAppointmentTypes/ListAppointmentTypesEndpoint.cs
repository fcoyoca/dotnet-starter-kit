using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.ListAppointmentTypes;

public static class ListAppointmentTypesEndpoint
{
    internal static RouteHandlerBuilder MapListAppointmentTypesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/appointment-types",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListAppointmentTypesQuery(isActive), ct))
            .WithName("ListAppointmentTypes")
            .WithSummary("List appointment types")
            .RequirePermission(AdministrationPermissions.AppointmentTypes.View);
    }
}
