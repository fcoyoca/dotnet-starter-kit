using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Claims.Contracts.Authorization;

public static class ClaimsPermissions
{
    public const string Resource = "Claims";
    public const string View   = $"Permissions.{Resource}.View";
    public const string Manage = $"Permissions.{Resource}.Manage";

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Claims",   ActionConstants.View, Resource, IsBasic: true),
        new("Manage Claims", "Manage",             Resource),
    ];
}
