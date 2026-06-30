using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ConfirmAppointment;

public static class ConfirmAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapConfirmAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments/{id:guid}/confirm",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ConfirmAppointmentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("ConfirmAppointment")
            .WithSummary("Confirm an appointment")
            .RequirePermission(SchedulingPermissions.Appointments.Update);
    }
}
