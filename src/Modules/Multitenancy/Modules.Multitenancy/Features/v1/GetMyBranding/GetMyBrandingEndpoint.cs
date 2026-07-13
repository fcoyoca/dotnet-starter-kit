using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Contracts.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.GetMyBranding;

/// <summary>
/// Branding for the calling tenant, readable by any authenticated user of that tenant.
/// </summary>
/// <remarks>
/// Distinct from <c>GET /theme</c>, which gates on <c>Tenants.ViewTheme</c> because it
/// exposes the full editable theme. Every user needs the wordmark and logo to render
/// the app chrome, so this returns only that slice and requires nothing but auth.
/// </remarks>
public static class GetMyBrandingEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/me/branding", async (
                IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
                ITenantThemeService themeService,
                CancellationToken cancellationToken) =>
            {
                var tenant = tenantAccessor.MultiTenantContext?.TenantInfo;
                if (tenant is null || string.IsNullOrEmpty(tenant.Id))
                {
                    return Results.Unauthorized();
                }

                var theme = await themeService.GetThemeAsync(tenant.Id, cancellationToken).ConfigureAwait(false);

                // AppName is the operator-set override; the tenant's own Name is the
                // sensible default. Null when neither is set — the client then renders
                // the framework wordmark.
                //
                // Root is the framework's own tenant, not a customer, so its Name
                // ("Root") is not a brand — leave it null so root keeps the default
                // lockup unless an operator deliberately sets an AppName for it.
                var isRoot = string.Equals(tenant.Id, MultitenancyConstants.Root.Id, StringComparison.Ordinal);
                var appName = theme.AppName;
                if (string.IsNullOrWhiteSpace(appName))
                {
                    appName = isRoot ? null : tenant.Name;
                }

                return Results.Ok(new TenantBrandingDto
                {
                    AppName = string.IsNullOrWhiteSpace(appName) ? null : appName,
                    LogoUrl = theme.BrandAssets.LogoUrl,
                    LogoDarkUrl = theme.BrandAssets.LogoDarkUrl,
                    FaviconUrl = theme.BrandAssets.FaviconUrl
                });
            })
            .WithName("GetMyBranding")
            .WithSummary("Get the calling tenant's branding")
            .WithDescription("Returns the app name and logo URLs for the authenticated tenant — used by the tenant dashboard to render its chrome. Requires no permission beyond authentication.")
            .RequireAuthorization()
            .Produces<TenantBrandingDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
