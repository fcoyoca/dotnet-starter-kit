using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.DeletePatientInsurancePolicy;

public static class DeletePatientInsurancePolicyEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientInsurancePolicyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/insurance-policies/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientInsurancePolicyCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePatientInsurancePolicy")
            .WithSummary("Remove a patient's insurance policy")
            .RequirePermission(PatientPermissions.InsurancePolicies.Delete);
    }
}
