using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Scheduling.Contracts.Authorization;

public static class SchedulingPermissions
{
    public static class Appointments
    {
        public const string Resource = "Scheduling.Appointments";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Appointments",   ActionConstants.View,   Appointments.Resource, IsBasic: true),
        new("Create Appointments", ActionConstants.Create, Appointments.Resource),
        new("Update Appointments", ActionConstants.Update, Appointments.Resource),
        new("Delete Appointments", ActionConstants.Delete, Appointments.Resource),
    ];
}
