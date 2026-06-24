using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.UpdateAppointment;

public static class UpdateAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapUpdateAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/appointments/{id:guid}",
                async (Guid id, UpdateAppointmentCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (id != command.Id)
                    {
                        return Results.BadRequest("Route id does not match command id.");
                    }

                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateAppointment")
            .WithSummary("Update an appointment")
            .RequirePermission(SchedulingPermissions.Appointments.Update);
    }
}
