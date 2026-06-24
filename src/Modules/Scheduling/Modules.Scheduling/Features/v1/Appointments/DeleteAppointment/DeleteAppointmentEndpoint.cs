using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.DeleteAppointment;

public static class DeleteAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapDeleteAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/appointments/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteAppointmentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteAppointment")
            .WithSummary("Delete an appointment")
            .RequirePermission(SchedulingPermissions.Appointments.Delete);
    }
}
