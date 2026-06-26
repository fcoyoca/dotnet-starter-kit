using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.RequestReportReview;

public static class RequestReportReviewEndpoint
{
    internal static RouteHandlerBuilder MapRequestReportReviewEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/reports/{id:guid}/request-review",
                async (Guid id, RequestReportReviewCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    RequestReportReviewCommand command = body with { ReportId = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("RequestReportReview")
            .WithSummary("Request a review for a signed report")
            .RequirePermission(PatientPermissions.Reports.Review);
    }
}
