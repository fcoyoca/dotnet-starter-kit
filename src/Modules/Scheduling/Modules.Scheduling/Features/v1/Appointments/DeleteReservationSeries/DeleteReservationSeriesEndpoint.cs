using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.DeleteReservationSeries;

public static class DeleteReservationSeriesEndpoint
{
    internal static RouteHandlerBuilder MapDeleteReservationSeriesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapDelete("/appointments/series/{seriesId:guid}",
                async (Guid seriesId, IMediator mediator, CancellationToken ct) =>
                {
                    int count = await mediator.Send(new DeleteReservationSeriesCommand(seriesId), ct);
                    return Results.Ok(count);
                })
            .WithName("DeleteReservationSeries")
            .WithSummary("Delete an entire recurring reserve-time series")
            .RequirePermission(SchedulingPermissions.Appointments.Delete);
    }
}
