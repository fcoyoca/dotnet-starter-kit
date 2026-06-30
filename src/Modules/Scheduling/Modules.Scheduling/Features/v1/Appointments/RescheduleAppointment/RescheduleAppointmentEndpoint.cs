using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.RescheduleAppointment;

/// <summary>Body for <c>POST /appointments/{id}/reschedule</c> — the route supplies the original appointment id.</summary>
public sealed record RescheduleAppointmentRequest(Guid ProviderId, DateTime StartUtc, DateTime EndUtc);

public static class RescheduleAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapRescheduleAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments/{id:guid}/reschedule",
                async (Guid id, RescheduleAppointmentRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(request);
                    Guid newId = await mediator.Send(
                        new RescheduleAppointmentCommand(id, request.ProviderId, request.StartUtc, request.EndUtc), ct);
                    return Results.Ok(newId);
                })
            .WithName("RescheduleAppointment")
            .WithSummary("Reschedule an appointment (creates a replacement and links the original)")
            .RequirePermission(SchedulingPermissions.Appointments.Update);
    }
}
