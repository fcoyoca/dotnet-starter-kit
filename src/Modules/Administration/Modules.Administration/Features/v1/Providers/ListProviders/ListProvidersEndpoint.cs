using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Providers;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Providers.ListProviders;

public static class ListProvidersEndpoint
{
    internal static RouteHandlerBuilder MapListProvidersEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/providers",
                async (
                    string? search,
                    bool? isActive,
                    Guid? primaryClinicId,
                    int? pageNumber,
                    int? pageSize,
                    string? sortBy,
                    string? sortDir,
                    IMediator mediator,
                    CancellationToken ct) =>
                    Results.Ok(await mediator.Send(
                        new ListProvidersQuery(search, isActive, primaryClinicId, pageNumber ?? 1, pageSize ?? 20, sortBy, sortDir), ct)))
            .WithName("ListProviders")
            .WithSummary("Search and list providers")
            .RequirePermission(AdministrationPermissions.Providers.View);
    }
}
