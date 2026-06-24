using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.ScheduleConfig;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.ScheduleConfig.GetScheduleConfig;

public static class GetScheduleConfigEndpoint
{
    internal static RouteHandlerBuilder MapGetScheduleConfigEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/schedule-config/{clinicId:guid}",
                (Guid clinicId, IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetScheduleConfigQuery(clinicId), ct))
            .WithName("GetScheduleConfig")
            .WithSummary("Get the schedule units for a clinic")
            .RequirePermission(AdministrationPermissions.ScheduleConfig.View);
    }
}
