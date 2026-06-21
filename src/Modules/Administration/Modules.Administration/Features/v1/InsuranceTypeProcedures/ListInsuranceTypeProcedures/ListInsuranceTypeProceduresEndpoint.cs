using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.ListInsuranceTypeProcedures;

public static class ListInsuranceTypeProceduresEndpoint
{
    internal static RouteHandlerBuilder MapListInsuranceTypeProceduresEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/insurance-types/{insuranceTypeId:guid}/procedures",
                (Guid insuranceTypeId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListInsuranceTypeProceduresQuery(insuranceTypeId), ct))
            .WithName("ListInsuranceTypeProcedures")
            .WithSummary("List the procedure codes associated with an insurance type")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.View);
    }
}
