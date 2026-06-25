using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.CreatePatientIncident;

public static class CreatePatientIncidentEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientIncidentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/incidents",
                async (CreatePatientIncidentCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientIncident")
            .WithSummary("Create a new patient incident")
            .RequirePermission(PatientPermissions.Incidents.Create);
    }
}
