using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.SearchPatientMedications;

public static class SearchPatientMedicationsEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientMedicationsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/medications",
                async (
                    Guid patientId,
                    bool? includeInactive,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientMedicationsQuery(
                            patientId,
                            includeInactive ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientMedications")
            .WithSummary("Search a patient's medication list")
            .RequirePermission(PatientPermissions.Medications.View);
    }
}
