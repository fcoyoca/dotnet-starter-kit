using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.DeleteSmokingStatus;

public static class DeleteSmokingStatusEndpoint
{
    internal static RouteHandlerBuilder MapDeleteSmokingStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/smoking-statuses/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteSmokingStatusCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteSmokingStatus")
            .WithSummary("Delete a smoking status")
            .RequirePermission(AdministrationPermissions.SmokingStatuses.Delete);
    }
}
