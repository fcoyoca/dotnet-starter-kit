using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CreateRecurringReservation;

public static class CreateRecurringReservationEndpoint
{
    internal static RouteHandlerBuilder MapCreateRecurringReservationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments/reserve-recurring",
                async (CreateRecurringReservationCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    int count = await mediator.Send(command, ct);
                    return Results.Ok(count);
                })
            .WithName("CreateRecurringReservation")
            .WithSummary("Create a recurring reserve-time series")
            .RequirePermission(SchedulingPermissions.Appointments.Create);
    }
}
