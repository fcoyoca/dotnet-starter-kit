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

    public static class Incidents
    {
        public const string Resource = "Patient.Incidents";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Close  = $"Permissions.{Resource}.Close";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Reports
    {
        public const string Resource = "Patient.Reports";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Sign   = $"Permissions.{Resource}.Sign";
        public const string Review = $"Permissions.{Resource}.Review";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Problems
    {
        public const string Resource = "Patient.Problems";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Allergies
    {
        public const string Resource = "Patient.Allergies";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Patients",    ActionConstants.View,   Patients.Resource, IsBasic: true),
        new("Create Patients",  ActionConstants.Create, Patients.Resource),
        new("Update Patients",  ActionConstants.Update, Patients.Resource),
        new("Delete Patients",  ActionConstants.Delete, Patients.Resource),
        new("Restore Patients", "Restore",              Patients.Resource),
        new("View Incidents",   ActionConstants.View,   Incidents.Resource, IsBasic: true),
        new("Create Incidents", ActionConstants.Create, Incidents.Resource),
        new("Update Incidents", ActionConstants.Update, Incidents.Resource),
        new("Close Incidents",  "Close",                Incidents.Resource),
        new("Delete Incidents", ActionConstants.Delete, Incidents.Resource),
        new("View Reports",   ActionConstants.View,   Reports.Resource, IsBasic: true),
        new("Create Reports", ActionConstants.Create, Reports.Resource),
        new("Update Reports", ActionConstants.Update, Reports.Resource),
        new("Sign Reports",   "Sign",                 Reports.Resource),
        new("Review Reports", "Review",               Reports.Resource),
        new("Delete Reports", ActionConstants.Delete, Reports.Resource),
        new("View Problems",   ActionConstants.View,   Problems.Resource, IsBasic: true),
        new("Create Problems", ActionConstants.Create, Problems.Resource),
        new("Update Problems", ActionConstants.Update, Problems.Resource),
        new("Delete Problems", ActionConstants.Delete, Problems.Resource),
        new("View Allergies",   ActionConstants.View,   Allergies.Resource, IsBasic: true),
        new("Create Allergies", ActionConstants.Create, Allergies.Resource),
        new("Update Allergies", ActionConstants.Update, Allergies.Resource),
        new("Delete Allergies", ActionConstants.Delete, Allergies.Resource),
    ];
}
