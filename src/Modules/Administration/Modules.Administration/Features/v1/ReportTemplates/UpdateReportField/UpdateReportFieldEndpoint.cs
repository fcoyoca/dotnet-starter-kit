using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.UpdateReportField;

public static class UpdateReportFieldEndpoint
{
    internal static RouteHandlerBuilder MapUpdateReportFieldEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/report-fields/{id:int}",
                async (int id, UpdateReportFieldCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (id != command.Id)
                    {
                        return Results.BadRequest("Route id does not match command id.");
                    }

                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateReportField")
            .WithSummary("Update a report field")
            .RequirePermission(AdministrationPermissions.ReportTemplates.Update);
    }
}
