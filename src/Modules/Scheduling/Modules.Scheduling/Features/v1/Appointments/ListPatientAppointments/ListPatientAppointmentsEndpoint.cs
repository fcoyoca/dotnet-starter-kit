using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListPatientAppointments;

public static class ListPatientAppointmentsEndpoint
{
    internal static RouteHandlerBuilder MapListPatientAppointmentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/appointments/by-patient/{patientId:guid}",
                (Guid patientId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListPatientAppointmentsQuery(patientId), ct))
            .WithName("ListPatientAppointments")
            .WithSummary("List a patient's appointments (newest first) for the report appointment picker")
            .RequirePermission(SchedulingPermissions.Appointments.View);
    }
}
