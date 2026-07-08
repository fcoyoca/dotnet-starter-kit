using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.ListCodes;

public static class ListDiagnosticCategoryCodesEndpoint
{
    internal static RouteHandlerBuilder MapListDiagnosticCategoryCodesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/diagnostic-categories/{id:guid}/codes",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListDiagnosticCategoryCodesQuery(id), ct))
            .WithName("ListDiagnosticCategoryCodes")
            .WithSummary("List the diagnostic codes associated with a diagnostic category")
            .RequirePermission(AdministrationPermissions.DiagnosticCategories.View);
    }
}
