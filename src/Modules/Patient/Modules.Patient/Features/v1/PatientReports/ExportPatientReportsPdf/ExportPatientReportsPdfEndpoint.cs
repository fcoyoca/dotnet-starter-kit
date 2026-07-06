using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.ExportPatientReportsPdf;

public static class ExportPatientReportsPdfEndpoint
{
    internal static RouteHandlerBuilder MapExportPatientReportsPdfEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/reports/export-pdf",
                async (ExportPatientReportsPdfQuery query, IMediator mediator, HttpContext httpContext, CancellationToken ct) =>
                {
                    var result = await mediator.Send(query, ct).ConfigureAwait(false);
                    // Integrity checksum for the client's "Secure Download" display (legacy parity).
                    httpContext.Response.Headers.Append("X-Content-Sha256", result.Sha256);
                    return Results.File(result.Content, "application/pdf", result.FileName);
                })
            .WithName("ExportPatientReportsPdf")
            .WithSummary("Export one or more patient reports as a single (merged) PDF")
            .RequirePermission(PatientPermissions.Reports.Export)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status404NotFound);
    }
}
