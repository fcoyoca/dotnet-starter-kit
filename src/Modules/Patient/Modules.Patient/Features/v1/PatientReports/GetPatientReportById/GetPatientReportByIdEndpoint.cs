using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Patient.Contracts.Authorization;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Patient.Features.v1.PatientReports.GetPatientReportById;

public static class GetPatientReportByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPatientReportByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/reports/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(new GetPatientReportByIdQuery(id), ct)))
            .WithName("GetPatientReportById")
            .WithSummary("Get a single patient report by ID")
            .RequirePermission(PatientPermissions.Reports.View);
    }
}
