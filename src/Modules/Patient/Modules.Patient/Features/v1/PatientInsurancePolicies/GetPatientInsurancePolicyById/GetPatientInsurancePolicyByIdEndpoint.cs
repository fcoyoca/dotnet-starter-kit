using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.GetPatientInsurancePolicyById;

public static class GetPatientInsurancePolicyByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientInsurancePolicyByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/insurance-policies/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientInsurancePolicyByIdQuery(id), ct)))
            .WithName("GetPatientInsurancePolicyById")
            .WithSummary("Get a single insurance policy")
            .RequirePermission(PatientPermissions.InsurancePolicies.View);
    }
}
