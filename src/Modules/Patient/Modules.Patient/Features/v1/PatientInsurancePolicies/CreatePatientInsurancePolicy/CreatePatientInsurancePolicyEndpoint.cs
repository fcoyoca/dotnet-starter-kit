using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.CreatePatientInsurancePolicy;

public static class CreatePatientInsurancePolicyEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientInsurancePolicyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/insurance-policies",
                async (CreatePatientInsurancePolicyCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientInsurancePolicy")
            .WithSummary("Add an insurance policy to a patient")
            .RequirePermission(PatientPermissions.InsurancePolicies.Create);
    }
}
