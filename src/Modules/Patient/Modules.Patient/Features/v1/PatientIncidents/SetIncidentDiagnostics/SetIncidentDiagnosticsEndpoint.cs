using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.SetIncidentDiagnostics;

public static class SetIncidentDiagnosticsEndpoint
{
    internal static RouteHandlerBuilder MapSetIncidentDiagnosticsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/incidents/{id:guid}/diagnostics",
                async (Guid id, SetIncidentDiagnosticsCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetIncidentDiagnosticsCommand command = body with { IncidentId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetIncidentDiagnostics")
            .WithSummary("Set the diagnostic codes associated with an incident")
            .RequirePermission(PatientPermissions.Incidents.Update);
    }
}
