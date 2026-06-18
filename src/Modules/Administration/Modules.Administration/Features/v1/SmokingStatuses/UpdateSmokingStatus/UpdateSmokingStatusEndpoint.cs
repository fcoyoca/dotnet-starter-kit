using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.UpdateSmokingStatus;

public static class UpdateSmokingStatusEndpoint
{
    internal static RouteHandlerBuilder MapUpdateSmokingStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/smoking-statuses/{id:int}",
                async (int id, UpdateSmokingStatusCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateSmokingStatusCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateSmokingStatus")
            .WithSummary("Update a smoking status")
            .RequirePermission(AdministrationPermissions.SmokingStatuses.Update);
    }
}
