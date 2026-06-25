using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.UpdatePatientIncident;

public static class UpdatePatientIncidentEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientIncidentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/incidents/{id:guid}",
                async (Guid id, UpdatePatientIncidentCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(command with { IncidentId = id }, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientIncident")
            .WithSummary("Update a patient incident")
            .RequirePermission(PatientPermissions.Incidents.Update);
    }
}
