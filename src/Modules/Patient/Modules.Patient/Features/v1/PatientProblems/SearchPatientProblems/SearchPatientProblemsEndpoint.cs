using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.SearchPatientProblems;

public static class SearchPatientProblemsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientProblemsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/problems",
                async (
                    Guid patientId,
                    bool? includeInactive,
                    bool? includeResolved,
                    bool? includeDeleted,
                    bool? medicalAlertsOnly,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientProblemsQuery(
                            patientId,
                            includeInactive ?? false,
                            includeResolved ?? false,
                            includeDeleted ?? false,
                            medicalAlertsOnly ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientProblems")
            .WithSummary("Search a patient's problem list")
            .RequirePermission(PatientPermissions.Problems.View);
    }
}
