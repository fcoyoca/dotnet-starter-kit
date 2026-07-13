using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.SearchPatientInsurancePolicies;

public static class SearchPatientInsurancePoliciesEndpoint
{
    internal static RouteHandlerBuilder MapSearchPatientInsurancePoliciesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/insurance-policies",
                async (
                    Guid patientId,
                    bool? includeInactive,
                    int? pageNumber,
                    int? pageSize,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new SearchPatientInsurancePoliciesQuery(
                            patientId,
                            includeInactive ?? false,
                            pageNumber ?? 1,
                            pageSize ?? 100), ct)))
            .WithName("SearchPatientInsurancePolicies")
            .WithSummary("List a patient's insurance policies in coordination-of-benefits order")
            .RequirePermission(PatientPermissions.InsurancePolicies.View);
    }
}
