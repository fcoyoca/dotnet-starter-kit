using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Clinics.CreateClinic;

public static class CreateClinicEndpoint
{
    internal static RouteHandlerBuilder MapCreateClinicEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/clinics",
                async (CreateClinicCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateClinic")
            .WithSummary("Create a clinic")
            .RequirePermission(AdministrationPermissions.Clinics.Create)
            .WithIdempotency();
    }
}
