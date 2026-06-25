using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CheckOutAppointment;

public static class CheckOutAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapCheckOutAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments/{id:guid}/check-out",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new CheckOutAppointmentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("CheckOutAppointment")
            .WithSummary("Check out an appointment")
            .RequirePermission(SchedulingPermissions.Appointments.Update);
    }
}
