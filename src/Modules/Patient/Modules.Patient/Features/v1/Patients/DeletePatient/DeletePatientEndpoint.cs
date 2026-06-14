using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.DeletePatient;

public static class DeletePatientEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/patients/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePatient")
            .WithSummary("Soft-delete a patient")
            .RequirePermission(PatientPermissions.Patients.Delete);
    }
}
