using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Web.Idempotency;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.CreateAppointmentType;

public static class CreateAppointmentTypeEndpoint
{
    internal static RouteHandlerBuilder MapCreateAppointmentTypeEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPost("/appointment-types",
                async (CreateAppointmentTypeCommand command, IMediator mediator, CancellationToken ct) =>
                    Results.Ok(await mediator.Send(command, ct)))
            .WithName("CreateAppointmentType")
            .WithSummary("Create an appointment type")
            .RequirePermission(AdministrationPermissions.AppointmentTypes.Create)
            .WithIdempotency();
    }
}
