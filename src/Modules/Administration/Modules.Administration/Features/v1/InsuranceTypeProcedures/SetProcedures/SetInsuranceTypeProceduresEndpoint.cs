using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.SetProcedures;

public static class SetInsuranceTypeProceduresEndpoint
{
    internal static RouteHandlerBuilder MapSetInsuranceTypeProceduresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/insurance-types/{insuranceTypeId:guid}/procedures",
                async (Guid insuranceTypeId, SetInsuranceTypeProceduresCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    SetInsuranceTypeProceduresCommand command = body with { InsuranceTypeId = insuranceTypeId };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("SetInsuranceTypeProcedures")
            .WithSummary("Replace the procedure-code price associations for an insurance type")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.Update);
    }
}
