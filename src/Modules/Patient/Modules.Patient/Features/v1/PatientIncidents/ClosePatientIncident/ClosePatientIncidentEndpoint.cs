using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.ClosePatientIncident;

public static class ClosePatientIncidentEndpoint
{
    internal static RouteHandlerBuilder MapClosePatientIncidentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/incidents/{id:guid}/close",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ClosePatientIncidentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("ClosePatientIncident")
            .WithSummary("Close a patient incident")
            .RequirePermission(PatientPermissions.Incidents.Close);
    }
}
