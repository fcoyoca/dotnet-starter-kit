using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.SearchPatientAllergies;

public static class SearchPatientAllergiesEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientAllergiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/allergies",
                async (
                    Guid patientId,
                    bool? includeInactive,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientAllergiesQuery(
                            patientId,
                            includeInactive ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientAllergies")
            .WithSummary("Search a patient's allergy list")
            .RequirePermission(PatientPermissions.Allergies.View);
    }
}
