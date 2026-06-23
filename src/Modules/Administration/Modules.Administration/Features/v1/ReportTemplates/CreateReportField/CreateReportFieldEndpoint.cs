using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportField;

public static class CreateReportFieldEndpoint
{
    internal static RouteHandlerBuilder MapCreateReportFieldEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/report-fields",
                async (CreateReportFieldCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateReportField")
            .WithSummary("Create a report field")
            .RequirePermission(AdministrationPermissions.ReportTemplates.Create)
            .WithIdempotency();
    }
}
