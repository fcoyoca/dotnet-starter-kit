using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.GetAppointmentById;

public static class GetAppointmentByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetAppointmentByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/appointments/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetAppointmentByIdQuery(id), ct))
            .WithName("GetAppointmentById")
            .WithSummary("Get an appointment by id")
            .RequirePermission(SchedulingPermissions.Appointments.View);
    }
}
