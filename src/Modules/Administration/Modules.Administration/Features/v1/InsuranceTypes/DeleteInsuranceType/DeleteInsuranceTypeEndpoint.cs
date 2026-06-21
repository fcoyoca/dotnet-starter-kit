using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.DeleteInsuranceType;

public static class DeleteInsuranceTypeEndpoint
{
    internal static RouteHandlerBuilder MapDeleteInsuranceTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/insurance-types/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteInsuranceTypeCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteInsuranceType")
            .WithSummary("Delete an insurance type")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.Delete);
    }
}
