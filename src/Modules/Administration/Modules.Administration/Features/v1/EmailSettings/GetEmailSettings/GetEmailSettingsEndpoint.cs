using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Contracts.v1.EmailSettings;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Administration.Features.v1.EmailSettings.GetEmailSettings;

public static class GetEmailSettingsEndpoint
{
    internal static RouteHandlerBuilder MapGetEmailSettingsEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/email-settings",
                (IMediator mediator, CancellationToken ct) =>
                    mediator.Send(new GetEmailSettingsQuery(), ct))
            .WithName("GetEmailSettings")
            .WithSummary("Get the current tenant's email settings")
            .RequirePermission(AdministrationPermissions.EmailSettings.View);
    }
}
