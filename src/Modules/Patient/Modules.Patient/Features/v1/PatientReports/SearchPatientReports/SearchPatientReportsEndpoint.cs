using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SearchPatientReports;

public static class SearchPatientReportsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientReportsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/reports",
                async (
                    Guid? incidentId,
                    Guid? patientId,
                    string? search,
                    bool? includeDeleted,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientReportsQuery(
                            incidentId,
                            patientId,
                            search,
                            includeDeleted ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 50), ct)))
            .WithName("SearchPatientReports")
            .WithSummary("Search patient reports (by incident/patient, optional phrase search)")
            .RequirePermission(PatientPermissions.Reports.View);
    }
}
