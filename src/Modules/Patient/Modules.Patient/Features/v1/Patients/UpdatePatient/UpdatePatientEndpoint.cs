using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.UpdatePatient;

public static class UpdatePatientEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/patients/{id:guid}",
                async (Guid id, UpdatePatientCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command with { PatientId = id }, ct)))
            .WithName("UpdatePatient")
            .WithSummary("Update a patient")
            .RequirePermission(PatientPermissions.Patients.Update);
    }
}
