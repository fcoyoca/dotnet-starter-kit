using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.ListDiagnostics;

public static class ListDiagnosticsEndpoint
{
    internal static RouteHandlerBuilder MapListDiagnosticsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/diagnostics",
                async (
                    string? search,
                    int? codeSourceId,
                    bool? isActive,
                    bool? isChiropractic,
                    bool? isBillable,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    Guid? categoryId,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListDiagnosticsQuery(search, codeSourceId, isActive, isChiropractic, isBillable,
                            pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir, categoryId), ct)))
            .WithName("ListDiagnostics")
            .WithSummary("Search and list ICD diagnostics")
            .RequirePermission(AdministrationPermissions.Diagnostics.View);
    }
}
