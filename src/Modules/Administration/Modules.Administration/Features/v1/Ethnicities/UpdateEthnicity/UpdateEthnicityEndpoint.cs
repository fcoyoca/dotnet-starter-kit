using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.UpdateEthnicity;

public static class UpdateEthnicityEndpoint
{
    internal static RouteHandlerBuilder MapUpdateEthnicityEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/ethnicities/{id:int}",
                async (int id, UpdateEthnicityCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateEthnicityCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateEthnicity")
            .WithSummary("Update an ethnicity")
            .RequirePermission(AdministrationPermissions.Ethnicities.Update);
    }
}
