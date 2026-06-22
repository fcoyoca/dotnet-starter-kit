using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.UpdateIncidentType;

public static class UpdateIncidentTypeEndpoint
{
    internal static RouteHandlerBuilder MapUpdateIncidentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/incident-types/{id:guid}",
                async (Guid id, UpdateIncidentTypeCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateIncidentTypeCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateIncidentType")
            .WithSummary("Update an incident type")
            .RequirePermission(AdministrationPermissions.IncidentTypes.Update);
    }
}
