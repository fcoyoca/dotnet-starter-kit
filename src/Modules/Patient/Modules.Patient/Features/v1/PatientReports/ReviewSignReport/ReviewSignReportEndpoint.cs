using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.ReviewSignReport;

public static class ReviewSignReportEndpoint
{
    internal static RouteHandlerBuilder MapReviewSignReportEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}/review-sign",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new ReviewSignReportCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("ReviewSignReport")
            .WithSummary("Review-sign a report (attestation + snapshot of the reviewer provider's signature image)")
            .RequirePermission(PatientPermissions.Reports.Review);
    }
}
