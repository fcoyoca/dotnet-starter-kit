using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.Clinics.DeleteClinic;

public static class DeleteClinicEndpoint
{
    internal static RouteHandlerBuilder MapDeleteClinicEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/clinics/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteClinicCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteClinic")
            .WithSummary("Delete a clinic")
            .RequirePermission(AdministrationPermissions.Clinics.Delete);
    }
}
