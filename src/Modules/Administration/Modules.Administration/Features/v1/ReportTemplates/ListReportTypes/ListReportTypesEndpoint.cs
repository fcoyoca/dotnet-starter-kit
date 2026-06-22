using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.ListReportTypes;

public static class ListReportTypesEndpoint
{
    internal static RouteHandlerBuilder MapListReportTypesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/report-types",
                (IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListReportTypesQuery(), ct))
            .WithName("ListReportTypes")
            .WithSummary("List report template types (for the Macros admin)")
            .RequirePermission(AdministrationPermissions.Macros.View);
    }
}
