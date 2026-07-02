using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.UpdateAllergyReaction;

public static class UpdateAllergyReactionEndpoint
{
    internal static RouteHandlerBuilder MapUpdateAllergyReactionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/allergy-reactions/{id:int}",
                async (int id, UpdateAllergyReactionCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateAllergyReactionCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateAllergyReaction")
            .WithSummary("Update an allergy reaction")
            .RequirePermission(AdministrationPermissions.AllergyReactions.Update);
    }
}
