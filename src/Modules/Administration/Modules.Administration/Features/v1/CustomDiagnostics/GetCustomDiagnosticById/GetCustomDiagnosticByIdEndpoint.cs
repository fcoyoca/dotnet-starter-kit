using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.GetCustomDiagnosticById;

public static class GetCustomDiagnosticByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetCustomDiagnosticByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/custom-diagnostics/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetCustomDiagnosticByIdQuery(id), ct))
            .WithName("GetCustomDiagnosticById")
            .WithSummary("Get a custom diagnostic by id")
            .RequirePermission(AdministrationPermissions.CustomDiagnostics.View);
    }
}
