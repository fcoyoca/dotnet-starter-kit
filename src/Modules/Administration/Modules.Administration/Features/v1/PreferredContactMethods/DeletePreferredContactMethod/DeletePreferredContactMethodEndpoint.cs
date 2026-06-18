using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.DeletePreferredContactMethod;

public static class DeletePreferredContactMethodEndpoint
{
    internal static RouteHandlerBuilder MapDeletePreferredContactMethodEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/preferred-contact-methods/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeletePreferredContactMethodCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeletePreferredContactMethod")
            .WithSummary("Delete a preferred contact method")
            .RequirePermission(AdministrationPermissions.PreferredContactMethods.Delete);
    }
}
