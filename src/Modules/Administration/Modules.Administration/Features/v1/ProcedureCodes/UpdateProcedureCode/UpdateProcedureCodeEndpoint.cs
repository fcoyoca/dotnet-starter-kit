using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.UpdateProcedureCode;

public static class UpdateProcedureCodeEndpoint
{
    internal static RouteHandlerBuilder MapUpdateProcedureCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/procedure-codes/{id:guid}",
                async (Guid id, UpdateProcedureCodeCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateProcedureCodeCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateProcedureCode")
            .WithSummary("Update a procedure code")
            .RequirePermission(AdministrationPermissions.ProcedureCodes.Update);
    }
}
