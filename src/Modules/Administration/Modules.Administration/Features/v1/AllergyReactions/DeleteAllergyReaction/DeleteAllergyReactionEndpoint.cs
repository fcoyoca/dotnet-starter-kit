using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.DeleteAllergyReaction;

public static class DeleteAllergyReactionEndpoint
{
    internal static RouteHandlerBuilder MapDeleteAllergyReactionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/allergy-reactions/{id:int}",
                async (int id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteAllergyReactionCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteAllergyReaction")
            .WithSummary("Delete an allergy reaction")
            .RequirePermission(AdministrationPermissions.AllergyReactions.Delete);
    }
}
