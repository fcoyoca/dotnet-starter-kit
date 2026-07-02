using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.ListAllergyReactions;

public static class ListAllergyReactionsEndpoint
{
    internal static RouteHandlerBuilder MapListAllergyReactionsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/allergy-reactions",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListAllergyReactionsQuery(isActive), ct))
            .WithName("ListAllergyReactions")
            .WithSummary("List all allergy reactions")
            .RequirePermission(AdministrationPermissions.AllergyReactions.View);
    }
}
