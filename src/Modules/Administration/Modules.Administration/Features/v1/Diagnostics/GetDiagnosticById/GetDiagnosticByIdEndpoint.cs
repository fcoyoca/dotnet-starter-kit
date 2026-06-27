using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.GetDiagnosticById;

public static class GetDiagnosticByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetDiagnosticByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/diagnostics/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetDiagnosticByIdQuery(id), ct))
            .WithName("GetDiagnosticById")
            .WithSummary("Get an ICD diagnostic by id")
            .RequirePermission(AdministrationPermissions.Diagnostics.View);
    }
}
