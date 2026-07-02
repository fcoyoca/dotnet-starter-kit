using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;

public static class CreateDrugEndpoint
{
    internal static RouteHandlerBuilder MapCreateDrugEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/drugs",
                async (CreateDrugCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateDrug")
            .WithSummary("Create a drug catalog entry")
            .RequirePermission(AdministrationPermissions.Drugs.Create)
            .WithIdempotency();
    }
}
