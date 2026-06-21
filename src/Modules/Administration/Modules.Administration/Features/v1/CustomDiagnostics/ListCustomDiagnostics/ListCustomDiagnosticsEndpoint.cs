using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.ListCustomDiagnostics;

public static class ListCustomDiagnosticsEndpoint
{
    internal static RouteHandlerBuilder MapListCustomDiagnosticsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/custom-diagnostics",
                async (
                    string? search,
                    bool? isActive,
                    bool? isChiropractic,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListCustomDiagnosticsQuery(search, isActive, isChiropractic, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListCustomDiagnostics")
            .WithSummary("Search and list custom diagnostics")
            .RequirePermission(AdministrationPermissions.CustomDiagnostics.View);
    }
}
