using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.UpdatePatientInsurancePolicy;

public static class UpdatePatientInsurancePolicyEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientInsurancePolicyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/insurance-policies/{id:guid}",
                async (Guid id, UpdatePatientInsurancePolicyCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePatientInsurancePolicyCommand command = body with { PolicyId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientInsurancePolicy")
            .WithSummary("Update a patient's insurance policy (including Active/Inactive)")
            .RequirePermission(PatientPermissions.InsurancePolicies.Update);
    }
}
