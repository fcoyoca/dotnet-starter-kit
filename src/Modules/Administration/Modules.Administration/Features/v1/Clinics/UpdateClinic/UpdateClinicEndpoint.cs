using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Clinics.UpdateClinic;

public static class UpdateClinicEndpoint
{
    internal static RouteHandlerBuilder MapUpdateClinicEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/clinics/{id:guid}",
                async (Guid id, UpdateClinicCommand body, IMediator mediator, CancellationToken ct) =>
                {
                    ArgumentNullException.ThrowIfNull(body);
                    UpdateClinicCommand command = body with { Id = id };
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateClinic")
            .WithSummary("Update a clinic")
            .RequirePermission(AdministrationPermissions.Clinics.Update);
    }
}
