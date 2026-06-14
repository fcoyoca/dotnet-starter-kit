using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.CreatePatient;

public static class CreatePatientEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/patients",
                async (CreatePatientCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatient")
            .WithSummary("Create a patient")
            .RequirePermission(PatientPermissions.Patients.Create)
            .WithIdempotency();
    }
}
