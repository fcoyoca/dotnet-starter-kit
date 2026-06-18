using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.ListPreferredContactMethods;

public static class ListPreferredContactMethodsEndpoint
{
    internal static RouteHandlerBuilder MapListPreferredContactMethodsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/preferred-contact-methods",
                (bool? isActive, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new ListPreferredContactMethodsQuery(isActive), ct))
            .WithName("ListPreferredContactMethods")
            .WithSummary("List all preferred contact methods")
            .RequirePermission(AdministrationPermissions.PreferredContactMethods.View);
    }
}
