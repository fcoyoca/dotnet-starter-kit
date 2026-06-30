using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.SearchPatients;

public static class SearchPatientsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/patients",
                async (
                    string? search,
                    string? ssnHash,
                    bool? isActive,
                    int pageNumber,
                    int pageSize,
                    string? sortBy,
                    string? sortDir,
                    Guid? providerId,
                    Guid? clinicId,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientsQuery(
                            search, ssnHash, isActive, pageNumber, pageSize,
                            sortBy, sortDir, providerId, clinicId), ct)))
            .WithName("SearchPatients")
            .WithSummary("Search and list patients (no PHI in response)")
            .RequirePermission(PatientPermissions.Patients.View);
    }
}
