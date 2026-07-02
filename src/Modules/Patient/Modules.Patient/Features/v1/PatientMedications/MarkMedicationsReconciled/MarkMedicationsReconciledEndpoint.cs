using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.MarkMedicationsReconciled;

public static class MarkMedicationsReconciledEndpoint
{
    internal static RouteHandlerBuilder MapMarkMedicationsReconciledEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/medication-reconciliations",
                async (MarkMedicationsReconciledCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("MarkMedicationsReconciled")
            .WithSummary("Mark a patient's medication list as reconciled today")
            .RequirePermission(PatientPermissions.Medications.Update);
    }
}
