using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.RestorePatient;

public static class RestorePatientEndpoint
{
    internal static RouteHandlerBuilder MapRestorePatientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/patients/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RestorePatientCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("RestorePatient")
            .WithSummary("Restore a soft-deleted patient")
            .RequirePermission(PatientPermissions.Patients.Restore);
    }
}
