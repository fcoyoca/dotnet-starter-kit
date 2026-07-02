using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;

public static class UpdatePatientMedicationEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientMedicationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/medications/{id:guid}",
                async (Guid id, UpdatePatientMedicationCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePatientMedicationCommand command = body with { MedicationId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientMedication")
            .WithSummary("Update a medication-list entry (including Active/Inactive)")
            .RequirePermission(PatientPermissions.Medications.Update);
    }
}
