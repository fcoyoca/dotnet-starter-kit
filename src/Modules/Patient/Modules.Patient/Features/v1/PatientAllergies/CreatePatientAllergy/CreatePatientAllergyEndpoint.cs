using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.CreatePatientAllergy;

public static class CreatePatientAllergyEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientAllergyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/allergies",
                async (CreatePatientAllergyCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientAllergy")
            .WithSummary("Add an allergy to a patient's allergy list")
            .RequirePermission(PatientPermissions.Allergies.Create);
    }
}
