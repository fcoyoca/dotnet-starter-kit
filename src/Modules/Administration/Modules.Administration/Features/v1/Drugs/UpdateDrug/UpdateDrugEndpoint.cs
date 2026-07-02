using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.UpdateDrug;

public static class UpdateDrugEndpoint
{
    internal static RouteHandlerBuilder MapUpdateDrugEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/drugs/{id:int}",
                async (int id, UpdateDrugCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateDrugCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateDrug")
            .WithSummary("Update a drug catalog entry")
            .RequirePermission(AdministrationPermissions.Drugs.Update);
    }
}
