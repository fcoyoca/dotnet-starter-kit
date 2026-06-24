using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListAppointments;

public static class ListAppointmentsEndpoint
{
    internal static RouteHandlerBuilder MapListAppointmentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/appointments",
                (Guid clinicId, DateTime fromUtc, DateTime toUtc, Guid[]? providerIds, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListAppointmentsQuery(clinicId, providerIds, fromUtc, toUtc), ct))
            .WithName("ListAppointments")
            .WithSummary("List appointments in a date range for a clinic")
            .RequirePermission(SchedulingPermissions.Appointments.View);
    }
}
