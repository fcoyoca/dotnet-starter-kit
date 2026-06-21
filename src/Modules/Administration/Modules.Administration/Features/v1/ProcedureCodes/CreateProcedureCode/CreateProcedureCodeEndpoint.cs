using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.CreateProcedureCode;

public static class CreateProcedureCodeEndpoint
{
    internal static RouteHandlerBuilder MapCreateProcedureCodeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/procedure-codes",
                async (CreateProcedureCodeCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateProcedureCode")
            .WithSummary("Create a procedure code")
            .RequirePermission(AdministrationPermissions.ProcedureCodes.Create)
            .WithIdempotency();
    }
}
