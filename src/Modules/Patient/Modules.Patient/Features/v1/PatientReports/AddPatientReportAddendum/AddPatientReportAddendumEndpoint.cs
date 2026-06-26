using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.AddPatientReportAddendum;

public static class AddPatientReportAddendumEndpoint
{
    internal static RouteHandlerBuilder MapAddPatientReportAddendumEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/reports/{id:guid}/addendums",
                async (Guid id, AddPatientReportAddendumCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    AddPatientReportAddendumCommand command = body with { ReportId = id };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("AddPatientReportAddendum")
            .WithSummary("Append an addendum to a report")
            .RequirePermission(PatientPermissions.Reports.Update);
    }
}
