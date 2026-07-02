using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.GetMedicationReconciledDates;

public static class GetMedicationReconciledDatesEndpoint
{
    internal static RouteHandlerBuilder MapGetMedicationReconciledDatesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/medication-reconciliations",
                async (Guid patientId, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetMedicationReconciledDatesQuery(patientId), ct)))
            .WithName("GetMedicationReconciledDates")
            .WithSummary("Get a patient's medication reconciliation history")
            .RequirePermission(PatientPermissions.Medications.View);
    }
}
