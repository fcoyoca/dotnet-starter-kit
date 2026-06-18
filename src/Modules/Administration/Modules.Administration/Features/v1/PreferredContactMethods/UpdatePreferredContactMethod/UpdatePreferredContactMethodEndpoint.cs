using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.UpdatePreferredContactMethod;

public static class UpdatePreferredContactMethodEndpoint
{
    internal static RouteHandlerBuilder MapUpdatePreferredContactMethodEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/preferred-contact-methods/{id:int}",
                async (int id, UpdatePreferredContactMethodCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdatePreferredContactMethodCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdatePreferredContactMethod")
            .WithSummary("Update a preferred contact method")
            .RequirePermission(AdministrationPermissions.PreferredContactMethods.Update);
    }
}
