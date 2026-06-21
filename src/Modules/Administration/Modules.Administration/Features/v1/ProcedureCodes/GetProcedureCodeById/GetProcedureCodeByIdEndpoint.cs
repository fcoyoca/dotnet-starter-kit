using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.GetProcedureCodeById;

public static class GetProcedureCodeByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetProcedureCodeByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/procedure-codes/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetProcedureCodeByIdQuery(id), ct))
            .WithName("GetProcedureCodeById")
            .WithSummary("Get a procedure code by id")
            .RequirePermission(AdministrationPermissions.ProcedureCodes.View);
    }
}
