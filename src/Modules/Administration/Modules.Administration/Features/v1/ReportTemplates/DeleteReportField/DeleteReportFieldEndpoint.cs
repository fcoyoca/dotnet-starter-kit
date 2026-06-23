using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportField;

public static class DeleteReportFieldEndpoint
{
    internal static RouteHandlerBuilder MapDeleteReportFieldEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/report-fields/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteReportFieldCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteReportField")
            .WithSummary("Delete a report field")
            .RequirePermission(AdministrationPermissions.ReportTemplates.Delete);
    }
}
