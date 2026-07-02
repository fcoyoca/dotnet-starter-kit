using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;

public static class CreatePatientMedicationEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientMedicationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/medications",
                async (CreatePatientMedicationCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientMedication")
            .WithSummary("Add a medication to a patient's medication list")
            .RequirePermission(PatientPermissions.Medications.Create);
    }
}
