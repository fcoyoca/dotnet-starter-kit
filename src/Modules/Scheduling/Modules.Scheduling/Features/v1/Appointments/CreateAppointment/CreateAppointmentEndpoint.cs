using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CreateAppointment;

public static class CreateAppointmentEndpoint
{
    internal static RouteHandlerBuilder MapCreateAppointmentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointments",
                async (CreateAppointmentCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    Guid id = await mediator.Send(command, ct);
                    return Results.Ok(id);
                })
            .WithName("CreateAppointment")
            .WithSummary("Create an appointment")
            .RequirePermission(SchedulingPermissions.Appointments.Create);
    }
}
