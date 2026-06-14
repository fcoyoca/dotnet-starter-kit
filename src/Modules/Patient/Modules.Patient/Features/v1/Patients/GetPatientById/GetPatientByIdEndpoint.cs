using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.Patients;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.Patients.GetPatientById;

public static class GetPatientByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/patients/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientByIdQuery(id), ct)))
            .WithName("GetPatientById")
            .WithSummary("Get a patient by ID (triggers HIPAA PHI access audit)")
            .RequirePermission(PatientPermissions.Patients.View);
    }
}
