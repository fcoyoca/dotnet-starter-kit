using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.CreatePatientReport;

public static class CreatePatientReportEndpoint
{
    internal static RouteHandlerBuilder MapCreatePatientReportEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/reports",
                async (CreatePatientReportCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePatientReport")
            .WithSummary("Create a new patient report (header only)")
            .RequirePermission(PatientPermissions.Reports.Create);
    }
}
