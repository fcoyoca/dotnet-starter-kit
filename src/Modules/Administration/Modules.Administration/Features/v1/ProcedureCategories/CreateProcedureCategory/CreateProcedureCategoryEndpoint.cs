using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.CreateProcedureCategory;

public static class CreateProcedureCategoryEndpoint
{
    internal static RouteHandlerBuilder MapCreateProcedureCategoryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/procedure-categories",
                async (CreateProcedureCategoryCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateProcedureCategory")
            .WithSummary("Create a procedure category")
            .RequirePermission(AdministrationPermissions.ProcedureCategories.Create)
            .WithIdempotency();
    }
}
