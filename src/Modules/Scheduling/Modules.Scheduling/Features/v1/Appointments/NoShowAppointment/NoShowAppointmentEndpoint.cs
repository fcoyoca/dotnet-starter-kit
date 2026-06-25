using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.NoShowAppointment;

public static class NoShowAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapNoShowAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments/{id:guid}/no-show",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new NoShowAppointmentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("NoShowAppointment")
            .WithSummary("Mark an appointment as a no-show")
            .RequirePermission(SchedulingPermissions.Appointments.Update);
    }
}
