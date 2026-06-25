using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.DeletePatientIncident;

public static class DeletePatientIncidentEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientIncidentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/incidents/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientIncidentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePatientIncident")
            .WithSummary("Soft-delete a patient incident")
            .RequirePermission(PatientPermissions.Incidents.Delete);
    }
}
