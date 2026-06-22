using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.DeleteIncidentType;

public static class DeleteIncidentTypeEndpoint
{
    internal static RouteHandlerBuilder MapDeleteIncidentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/incident-types/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteIncidentTypeCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteIncidentType")
            .WithSummary("Delete an incident type")
            .RequirePermission(AdministrationPermissions.IncidentTypes.Delete);
    }
}
