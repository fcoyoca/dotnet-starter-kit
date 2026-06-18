using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.CreateEthnicity;

public static class CreateEthnicityEndpoint
{
    internal static RouteHandlerBuilder MapCreateEthnicityEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/ethnicities",
                async (CreateEthnicityCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateEthnicity")
            .WithSummary("Create an ethnicity")
            .RequirePermission(AdministrationPermissions.Ethnicities.Create)
            .WithIdempotency();
    }
}
