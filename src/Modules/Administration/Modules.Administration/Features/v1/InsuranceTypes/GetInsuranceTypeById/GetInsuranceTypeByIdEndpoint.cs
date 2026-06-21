using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.GetInsuranceTypeById;

public static class GetInsuranceTypeByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetInsuranceTypeByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/insurance-types/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetInsuranceTypeByIdQuery(id), ct))
            .WithName("GetInsuranceTypeById")
            .WithSummary("Get an insurance type by id")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.View);
    }
}
