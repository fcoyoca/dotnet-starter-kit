using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SetReportProblems;

public static class SetReportProblemsEndpoint
{
    internal static RouteHandlerBuilder MapSetReportProblemsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}/problems",
                async (Guid id, SetReportProblemsCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetReportProblemsCommand command = body with { ReportId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetReportProblems")
            .WithSummary("Set the problems associated with a report")
            .RequirePermission(PatientPermissions.Reports.Update);
    }
}
