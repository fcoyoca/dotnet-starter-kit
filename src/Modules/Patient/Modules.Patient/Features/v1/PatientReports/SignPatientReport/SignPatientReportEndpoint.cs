using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SignPatientReport;

public static class SignPatientReportEndpoint
{
    internal static RouteHandlerBuilder MapSignPatientReportEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}/sign",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SignPatientReportCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("SignPatientReport")
            .WithSummary("Sign a report (attestation + snapshot of the provider's signature image)")
            .RequirePermission(PatientPermissions.Reports.Sign);
    }
}
