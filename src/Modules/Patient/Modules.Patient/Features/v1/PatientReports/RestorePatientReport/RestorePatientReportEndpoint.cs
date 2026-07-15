using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.RestorePatientReport;

public static class RestorePatientReportEndpoint
{
    internal static RouteHandlerBuilder MapRestorePatientReportEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}/restore",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RestorePatientReportCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("RestorePatientReport")
            .WithSummary("Restore a soft-deleted patient report")
            .RequirePermission(PatientPermissions.Reports.Restore);
    }
}
