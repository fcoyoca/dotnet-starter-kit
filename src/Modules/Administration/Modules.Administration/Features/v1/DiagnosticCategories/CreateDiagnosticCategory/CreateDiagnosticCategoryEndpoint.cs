using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.CreateDiagnosticCategory;

public static class CreateDiagnosticCategoryEndpoint
{
    internal static RouteHandlerBuilder MapCreateDiagnosticCategoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/diagnostic-categories",
                async (CreateDiagnosticCategoryCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateDiagnosticCategory")
            .WithSummary("Create a diagnostic category")
            .RequirePermission(AdministrationPermissions.DiagnosticCategories.Create)
            .WithIdempotency();
    }
}
