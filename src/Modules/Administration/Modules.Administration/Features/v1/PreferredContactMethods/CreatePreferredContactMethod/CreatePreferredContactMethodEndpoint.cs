using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.CreatePreferredContactMethod;

public static class CreatePreferredContactMethodEndpoint
{
    internal static RouteHandlerBuilder MapCreatePreferredContactMethodEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/preferred-contact-methods",
                async (CreatePreferredContactMethodCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreatePreferredContactMethod")
            .WithSummary("Create a preferred contact method")
            .RequirePermission(AdministrationPermissions.PreferredContactMethods.Create)
            .WithIdempotency();
    }
}
