using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.ListReportFields;

public static class ListReportFieldsEndpoint
{
    internal static RouteHandlerBuilder MapListReportFieldsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/report-fields",
                (int reportTypeId, bool? includeInactive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListReportFieldsQuery(reportTypeId, includeInactive ?? false), ct))
            .WithName("ListReportFields")
            .WithSummary("List report fields for a report type")
            .RequirePermission(AdministrationPermissions.ReportTemplates.View);
    }
}
