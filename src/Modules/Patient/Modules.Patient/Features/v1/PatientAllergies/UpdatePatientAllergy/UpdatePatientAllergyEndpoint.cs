using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.UpdatePatientAllergy;

public static class UpdatePatientAllergyEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientAllergyEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/allergies/{id:guid}",
                async (Guid id, UpdatePatientAllergyCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePatientAllergyCommand command = body with { AllergyId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientAllergy")
            .WithSummary("Update an allergy-list entry (including Active/Inactive)")
            .RequirePermission(PatientPermissions.Allergies.Update);
    }
}
