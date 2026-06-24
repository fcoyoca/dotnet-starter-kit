using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ScheduleConfig;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ScheduleConfig.UpsertScheduleConfig;

public static class UpsertScheduleConfigEndpoint
{
    internal static RouteHandlerBuilder MapUpsertScheduleConfigEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/schedule-config/{clinicId:guid}",
                async (Guid clinicId, UpsertScheduleConfigCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    if (clinicId != command.ClinicId)
                    {
                        return Results.BadRequest("Route clinicId does not match command clinicId.");
                    }

                    ScheduleConfigDto dto = await mediator.Send(command, ct);
                    return Results.Ok(dto);
                })
            .WithName("UpsertScheduleConfig")
            .WithSummary("Create or update the schedule units for a clinic")
            .RequirePermission(AdministrationPermissions.ScheduleConfig.Update);
    }
}
