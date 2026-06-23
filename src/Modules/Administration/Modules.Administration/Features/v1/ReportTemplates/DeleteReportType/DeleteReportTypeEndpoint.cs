using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportType;

public static class DeleteReportTypeEndpoint
{
    internal static RouteHandlerBuilder MapDeleteReportTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/report-types/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteReportTypeCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteReportType")
            .WithSummary("Delete a report type")
            .RequirePermission(AdministrationPermissions.ReportTemplates.Delete);
    }
}
