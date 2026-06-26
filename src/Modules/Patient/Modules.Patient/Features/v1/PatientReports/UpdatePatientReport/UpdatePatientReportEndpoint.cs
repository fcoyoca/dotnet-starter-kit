using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.UpdatePatientReport;

public static class UpdatePatientReportEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePatientReportEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}",
                async (Guid id, UpdatePatientReportCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePatientReportCommand command = body with { ReportId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePatientReport")
            .WithSummary("Update a draft patient report (header + vitals + field values)")
            .RequirePermission(PatientPermissions.Reports.Update);
    }
}
