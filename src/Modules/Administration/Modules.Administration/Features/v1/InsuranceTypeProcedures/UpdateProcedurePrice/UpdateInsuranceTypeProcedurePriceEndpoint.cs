using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.UpdateProcedurePrice;

public static class UpdateInsuranceTypeProcedurePriceEndpoint
{
    internal static RouteHandlerBuilder MapUpdateInsuranceTypeProcedurePriceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/insurance-types/{insuranceTypeId:guid}/procedures/{procedureCodeId:guid}",
                async (Guid insuranceTypeId, Guid procedureCodeId, UpdateInsuranceTypeProcedurePriceCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateInsuranceTypeProcedurePriceCommand command =
                        body with { InsuranceTypeId = insuranceTypeId, ProcedureCodeId = procedureCodeId };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateInsuranceTypeProcedurePrice")
            .WithSummary("Update the price of a procedure code associated with an insurance type")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.Update);
    }
}
