using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.RemoveProcedure;

public static class RemoveProcedureFromInsuranceTypeEndpoint
{
    internal static RouteHandlerBuilder MapRemoveProcedureFromInsuranceTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/insurance-types/{insuranceTypeId:guid}/procedures/{procedureCodeId:guid}",
                async (Guid insuranceTypeId, Guid procedureCodeId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new RemoveProcedureFromInsuranceTypeCommand(insuranceTypeId, procedureCodeId), ct);
                    return Results.NoContent();
                })
            .WithName("RemoveProcedureFromInsuranceType")
            .WithSummary("Remove a procedure code association from an insurance type")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.Update);
    }
}
