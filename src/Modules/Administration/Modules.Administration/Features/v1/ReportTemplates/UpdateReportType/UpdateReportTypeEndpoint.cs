using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.UpdateReportType;

public static class UpdateReportTypeEndpoint
{
    internal static RouteHandlerBuilder MapUpdateReportTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/report-types/{id:int}",
                async (int id, UpdateReportTypeCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (id != command.Id)
                    {
                        return Results.BadRequest("Route id does not match command id.");
                    }

                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateReportType")
            .WithSummary("Update a report type")
            .RequirePermission(AdministrationPermissions.ReportTemplates.Update);
    }
}
