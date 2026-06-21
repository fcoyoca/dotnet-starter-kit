using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.GetDiagnosticCategoryById;

public static class GetDiagnosticCategoryByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetDiagnosticCategoryByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/diagnostic-categories/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetDiagnosticCategoryByIdQuery(id), ct))
            .WithName("GetDiagnosticCategoryById")
            .WithSummary("Get a diagnostic category by id")
            .RequirePermission(AdministrationPermissions.DiagnosticCategories.View);
    }
}
