using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;

public static class SetPatientNoKnownAllergiesEndpoint
{
    public sealed record SetFlagRequest(bool Value);

    internal static RouteHandlerBuilder MapSetPatientNoKnownAllergiesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/patients/{id:guid}/no-known-allergies",
                async (Guid id, SetFlagRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SetPatientNoKnownAllergiesCommand(id, request.Value), ct);
                    return Results.NoContent();
                })
            .WithName("SetPatientNoKnownAllergies")
            .WithSummary("Set or clear the patient's No Known Allergies flag")
            .RequirePermission(PatientPermissions.Patients.Update);
    }
}
