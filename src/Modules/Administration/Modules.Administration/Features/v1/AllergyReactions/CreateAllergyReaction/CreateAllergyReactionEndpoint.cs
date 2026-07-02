using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.CreateAllergyReaction;

public static class CreateAllergyReactionEndpoint
{
    internal static RouteHandlerBuilder MapCreateAllergyReactionEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/allergy-reactions",
                async (CreateAllergyReactionCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateAllergyReaction")
            .WithSummary("Create an allergy reaction")
            .RequirePermission(AdministrationPermissions.AllergyReactions.Create)
            .WithIdempotency();
    }
}
