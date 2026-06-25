using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.GetPatientIncidentById;

public static class GetPatientIncidentByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientIncidentByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/incidents/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientIncidentByIdQuery(id), ct)))
            .WithName("GetPatientIncidentById")
            .WithSummary("Get a single patient incident by ID")
            .RequirePermission(PatientPermissions.Incidents.View);
    }
}
