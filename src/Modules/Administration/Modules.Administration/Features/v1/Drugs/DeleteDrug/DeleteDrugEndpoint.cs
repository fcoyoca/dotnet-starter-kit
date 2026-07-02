using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.DeleteDrug;

public static class DeleteDrugEndpoint
{
    internal static RouteHandlerBuilder MapDeleteDrugEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/drugs/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteDrugCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteDrug")
            .WithSummary("Soft-delete a drug catalog entry")
            .RequirePermission(AdministrationPermissions.Drugs.Delete);
    }
}
