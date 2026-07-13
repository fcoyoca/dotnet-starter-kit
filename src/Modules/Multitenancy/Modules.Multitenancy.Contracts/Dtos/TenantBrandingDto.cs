using System.ComponentModel;

namespace FSH.Modules.Multitenancy.Contracts.Dtos;

/// <summary>
/// The slice of a tenant's theme its own users are allowed to read without the
/// <c>Tenants.ViewTheme</c> permission — just enough to render the app chrome
/// (wordmark + logo). Deliberately excludes palettes, typography and layout so
/// this endpoint stays cheap and gives away nothing an ordinary user shouldn't see.
/// </summary>
/// <remarks>
/// Marked <see cref="ImmutableObjectAttribute"/> + <c>sealed</c> to match the
/// caching contract the other tenant DTOs follow.
/// </remarks>
[ImmutableObject(true)]
public sealed record TenantBrandingDto
{
    /// <summary>
    /// Name to render in place of the framework wordmark. Resolved server-side as
    /// the theme's AppName, falling back to the tenant's Name. Null only when the
    /// tenant has neither — clients then fall back to the framework default.
    /// </summary>
    public string? AppName { get; init; }

    public string? LogoUrl { get; init; }
    public string? LogoDarkUrl { get; init; }
    public string? FaviconUrl { get; init; }
}
