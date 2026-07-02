using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.GetPatientMedicationById;

public static class GetPatientMedicationByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientMedicationByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/medications/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientMedicationByIdQuery(id), ct)))
            .WithName("GetPatientMedicationById")
            .WithSummary("Get a single medication-list entry")
            .RequirePermission(PatientPermissions.Medications.View);
    }
}
