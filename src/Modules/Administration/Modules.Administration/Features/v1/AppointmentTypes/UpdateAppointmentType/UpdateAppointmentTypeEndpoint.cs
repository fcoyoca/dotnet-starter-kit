using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.UpdateAppointmentType;

public static class UpdateAppointmentTypeEndpoint
{
    internal static RouteHandlerBuilder MapUpdateAppointmentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/appointment-types/{id:guid}",
                async (Guid id, UpdateAppointmentTypeCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (id != command.Id)
                    {
                        return Results.BadRequest("Route id does not match command id.");
                    }

                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateAppointmentType")
            .WithSummary("Update an appointment type")
            .RequirePermission(AdministrationPermissions.AppointmentTypes.Update);
    }
}
