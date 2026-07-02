using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownMedications;

public static class SetPatientNoKnownMedicationsEndpoint
{
    public sealed record SetFlagRequest(bool Value);

    internal static RouteHandlerBuilder MapSetPatientNoKnownMedicationsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/patients/{id:guid}/no-known-medications",
                async (Guid id, SetFlagRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SetPatientNoKnownMedicationsCommand(id, request.Value), ct);
                    return Results.NoContent();
                })
            .WithName("SetPatientNoKnownMedications")
            .WithSummary("Set or clear the patient's No Known Medications flag")
            .RequirePermission(PatientPermissions.Patients.Update);
    }
}
