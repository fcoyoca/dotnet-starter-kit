using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.GetAllergyReactionById;

public static class GetAllergyReactionByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetAllergyReactionByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/allergy-reactions/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetAllergyReactionByIdQuery(id), ct))
            .WithName("GetAllergyReactionById")
            .WithSummary("Get an allergy reaction by id")
            .RequirePermission(AdministrationPermissions.AllergyReactions.View);
    }
}
