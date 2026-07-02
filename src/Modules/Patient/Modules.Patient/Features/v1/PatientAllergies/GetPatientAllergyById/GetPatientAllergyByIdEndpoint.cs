using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.GetPatientAllergyById;

public static class GetPatientAllergyByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientAllergyByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/allergies/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientAllergyByIdQuery(id), ct)))
            .WithName("GetPatientAllergyById")
            .WithSummary("Get a single allergy-list entry")
            .RequirePermission(PatientPermissions.Allergies.View);
    }
}
