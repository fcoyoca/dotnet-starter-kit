using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.AssociateProcedure;

public static class AddProcedureToInsuranceTypeEndpoint
{
    internal static RouteHandlerBuilder MapAddProcedureToInsuranceTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/insurance-types/{insuranceTypeId:guid}/procedures",
                async (Guid insuranceTypeId, AssociateProcedureToInsuranceTypeCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    AssociateProcedureToInsuranceTypeCommand command = body with { InsuranceTypeId = insuranceTypeId };
                    return Results.Ok(await mediator.Send(command, ct));
                })
            .WithName("AssociateProcedureToInsuranceType")
            .WithSummary("Associate a procedure code with an insurance type at a price")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.Update)
            .WithIdempotency();
    }
}
