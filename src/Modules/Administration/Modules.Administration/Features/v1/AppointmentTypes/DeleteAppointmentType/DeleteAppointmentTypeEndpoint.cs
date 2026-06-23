using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.DeleteAppointmentType;

public static class DeleteAppointmentTypeEndpoint
{
    internal static RouteHandlerBuilder MapDeleteAppointmentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/appointment-types/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new DeleteAppointmentTypeCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("DeleteAppointmentType")
            .WithSummary("Delete an appointment type")
            .RequirePermission(AdministrationPermissions.AppointmentTypes.Delete);
    }
}
