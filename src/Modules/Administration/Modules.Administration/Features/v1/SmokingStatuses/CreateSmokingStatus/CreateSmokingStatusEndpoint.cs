using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.CreateSmokingStatus;

public static class CreateSmokingStatusEndpoint
{
    internal static RouteHandlerBuilder MapCreateSmokingStatusEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/smoking-statuses",
                async (CreateSmokingStatusCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateSmokingStatus")
            .WithSummary("Create a smoking status")
            .RequirePermission(AdministrationPermissions.SmokingStatuses.Create)
            .WithIdempotency();
    }
}
