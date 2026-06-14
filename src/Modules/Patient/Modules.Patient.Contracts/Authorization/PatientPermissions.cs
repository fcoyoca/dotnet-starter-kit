using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Patient.Contracts.Authorization;

public static class PatientPermissions
{
    public static class Patients
    {
        public const string Resource = "Patient.Patients";
        public const string View    = $"Permissions.{Resource}.View";
        public const string Create  = $"Permissions.{Resource}.Create";
        public const string Update  = $"Permissions.{Resource}.Update";
        public const string Delete  = $"Permissions.{Resource}.Delete";
        public const string Restore = $"Permissions.{Resource}.Restore";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Patients",    ActionConstants.View,   Patients.Resource, IsBasic: true),
        new("Create Patients",  ActionConstants.Create, Patients.Resource),
        new("Update Patients",  ActionConstants.Update, Patients.Resource),
        new("Delete Patients",  ActionConstants.Delete, Patients.Resource),
        new("Restore Patients", "Restore",              Patients.Resource),
    ];
}
