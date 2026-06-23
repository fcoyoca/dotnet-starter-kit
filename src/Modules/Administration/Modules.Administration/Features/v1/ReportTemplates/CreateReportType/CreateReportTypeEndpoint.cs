using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportType;

public static class CreateReportTypeEndpoint
{
    internal static RouteHandlerBuilder MapCreateReportTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/report-types",
                async (CreateReportTypeCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateReportType")
            .WithSummary("Create a report type")
            .RequirePermission(AdministrationPermissions.ReportTemplates.Create)
            .WithIdempotency();
    }
}
