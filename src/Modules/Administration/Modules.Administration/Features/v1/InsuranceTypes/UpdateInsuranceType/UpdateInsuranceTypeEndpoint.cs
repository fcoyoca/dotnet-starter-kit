using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.UpdateInsuranceType;

public static class UpdateInsuranceTypeEndpoint
{
    internal static RouteHandlerBuilder MapUpdateInsuranceTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/insurance-types/{id:guid}",
                async (Guid id, UpdateInsuranceTypeCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateInsuranceTypeCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateInsuranceType")
            .WithSummary("Update an insurance type")
            .RequirePermission(AdministrationPermissions.InsuranceTypes.Update);
    }
}
