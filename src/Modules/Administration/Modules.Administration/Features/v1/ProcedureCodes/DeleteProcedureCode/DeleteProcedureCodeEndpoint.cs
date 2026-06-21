using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.DeleteProcedureCode;

public static class DeleteProcedureCodeEndpoint
{
    internal static RouteHandlerBuilder MapDeleteProcedureCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/procedure-codes/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteProcedureCodeCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteProcedureCode")
            .WithSummary("Delete a procedure code")
            .RequirePermission(AdministrationPermissions.ProcedureCodes.Delete);
    }
}
