using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.DeletePatientReport;

public static class DeletePatientReportEndpoint
{
    internal static RouteHandlerBuilder MapDeletePatientReportEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/reports/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePatientReportCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePatientReport")
            .WithSummary("Soft-delete a patient report")
            .RequirePermission(PatientPermissions.Reports.Delete);
    }
}
