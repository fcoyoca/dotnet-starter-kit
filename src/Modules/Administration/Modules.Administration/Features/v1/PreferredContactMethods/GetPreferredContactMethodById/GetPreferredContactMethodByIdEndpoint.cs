using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.GetPreferredContactMethodById;

public static class GetPreferredContactMethodByIdEndpoint
{
    internal static RouteHandlerBuilder MapGetPreferredContactMethodByIdEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/preferred-contact-methods/{id:int}",
                (int id, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetPreferredContactMethodByIdQuery(id), ct))
            .WithName("GetPreferredContactMethodById")
            .WithSummary("Get a preferred contact method by id")
            .RequirePermission(AdministrationPermissions.PreferredContactMethods.View);
    }
}
