using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.EmailSettings;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.EmailSettings.UpdateEmailSettings;

public static class UpdateEmailSettingsEndpoint
{
    internal static RouteHandlerBuilder MapUpdateEmailSettingsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/email-settings",
                async (UpdateEmailSettingsCommand command, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(command, ct);
                    return Results.NoContent();
                })
            .WithName("UpdateEmailSettings")
            .WithSummary("Update the current tenant's email settings")
            .RequirePermission(AdministrationPermissions.EmailSettings.Update);
    }
}
