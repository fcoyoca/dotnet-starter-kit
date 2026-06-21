using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.GetProcedureCategoryById;

public static class GetProcedureCategoryByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetProcedureCategoryByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/procedure-categories/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetProcedureCategoryByIdQuery(id), ct))
            .WithName("GetProcedureCategoryById")
            .WithSummary("Get a procedure category by id")
            .RequirePermission(AdministrationPermissions.ProcedureCategories.View);
    }
}
