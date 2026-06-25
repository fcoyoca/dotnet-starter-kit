using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.SearchPatientIncidents;

public static class SearchPatientIncidentsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientIncidentsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/incidents",
                async (
                    Guid patientId,
                    bool? isClosed,
                    bool includeDeleted,
                    int pageNumber,
                    int pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientIncidentsQuery(patientId, isClosed, includeDeleted, pageNumber, pageSize), ct)))
            .WithName("SearchPatientIncidents")
            .WithSummary("Search incidents for a patient")
            .RequirePermission(PatientPermissions.Incidents.View);
    }
}
