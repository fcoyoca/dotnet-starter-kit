using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.GetIncidentTypeById;

public static class GetIncidentTypeByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetIncidentTypeByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/incident-types/{id:guid}",
                (Guid id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetIncidentTypeByIdQuery(id), ct))
            .WithName("GetIncidentTypeById")
            .WithSummary("Get an incident type by id")
            .RequirePermission(AdministrationPermissions.IncidentTypes.View);
    }
}
