using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CheckInAppointment;

public static class CheckInAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapCheckInAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments/{id:guid}/check-in",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new CheckInAppointmentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("CheckInAppointment")
            .WithSummary("Check in an appointment")
            .RequirePermission(SchedulingPermissions.Appointments.Update);
    }
}
