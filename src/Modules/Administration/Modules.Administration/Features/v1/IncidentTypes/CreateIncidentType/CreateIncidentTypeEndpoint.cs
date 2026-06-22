using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.CreateIncidentType;

public static class CreateIncidentTypeEndpoint
{
    internal static RouteHandlerBuilder MapCreateIncidentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/incident-types",
                async (CreateIncidentTypeCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateIncidentType")
            .WithSummary("Create an incident type")
            .RequirePermission(AdministrationPermissions.IncidentTypes.Create)
            .WithIdempotency();
    }
}
