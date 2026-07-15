using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.RestorePatientIncident;

public static class RestorePatientIncidentEndpoint
{
    internal static RouteHandlerBuilder MapRestorePatientIncidentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/incidents/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RestorePatientIncidentCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("RestorePatientIncident")
            .WithSummary("Restore a soft-deleted patient incident")
            .RequirePermission(PatientPermissions.Incidents.Restore);
    }
}
