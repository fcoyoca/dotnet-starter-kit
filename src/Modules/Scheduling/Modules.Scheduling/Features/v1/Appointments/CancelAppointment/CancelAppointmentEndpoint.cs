using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CancelAppointment;

public static class CancelAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapCancelAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments/{id:guid}/cancel",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new CancelAppointmentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("CancelAppointment")
            .WithSummary("Cancel an appointment")
            .RequirePermission(SchedulingPermissions.Appointments.Update);
    }
}
