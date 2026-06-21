using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Administration.Contracts.Authorization;

public static class AdministrationPermissions
{
    public static class Clinics
    {
        public const string Resource = "Administration.Clinics";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Departments
    {
        public const string Resource = "Administration.Departments";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Providers
    {
        public const string Resource = "Administration.Providers";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class InsuranceTypes
    {
        public const string Resource = "Administration.InsuranceTypes";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class InsuranceCompanies
    {
        public const string Resource = "Administration.InsuranceCompanies";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class DiagnosticCategories
    {
        public const string Resource = "Administration.DiagnosticCategories";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class CustomDiagnostics
    {
        public const string Resource = "Administration.CustomDiagnostics";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class ProcedureCategories
    {
        public const string Resource = "Administration.ProcedureCategories";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class ProcedureCodes
    {
        public const string Resource = "Administration.ProcedureCodes";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Races
    {
        public const string Resource = "Administration.Races";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Ethnicities
    {
        public const string Resource = "Administration.Ethnicities";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Languages
    {
        public const string Resource = "Administration.Languages";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class SmokingStatuses
    {
        public const string Resource = "Administration.SmokingStatuses";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class PreferredContactMethods
    {
        public const string Resource = "Administration.PreferredContactMethods";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class ReferralTypes
    {
        public const string Resource = "Administration.ReferralTypes";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static IReadOnlyList<FshPermission> All { get; } =
    [
        new("View Clinics",   ActionConstants.View,   Clinics.Resource, IsBasic: true),
        new("Create Clinics", ActionConstants.Create, Clinics.Resource),
        new("Update Clinics", ActionConstants.Update, Clinics.Resource),
        new("Delete Clinics", ActionConstants.Delete, Clinics.Resource),

        new("View Departments",   ActionConstants.View,   Departments.Resource, IsBasic: true),
        new("Create Departments", ActionConstants.Create, Departments.Resource),
        new("Update Departments", ActionConstants.Update, Departments.Resource),
        new("Delete Departments", ActionConstants.Delete, Departments.Resource),

        new("View Providers",   ActionConstants.View,   Providers.Resource, IsBasic: true),
        new("Create Providers", ActionConstants.Create, Providers.Resource),
        new("Update Providers", ActionConstants.Update, Providers.Resource),
        new("Delete Providers", ActionConstants.Delete, Providers.Resource),

        new("View Insurance Types",   ActionConstants.View,   InsuranceTypes.Resource, IsBasic: true),
        new("Create Insurance Types", ActionConstants.Create, InsuranceTypes.Resource),
        new("Update Insurance Types", ActionConstants.Update, InsuranceTypes.Resource),
        new("Delete Insurance Types", ActionConstants.Delete, InsuranceTypes.Resource),

        new("View Insurance Companies",   ActionConstants.View,   InsuranceCompanies.Resource, IsBasic: true),
        new("Create Insurance Companies", ActionConstants.Create, InsuranceCompanies.Resource),
        new("Update Insurance Companies", ActionConstants.Update, InsuranceCompanies.Resource),
        new("Delete Insurance Companies", ActionConstants.Delete, InsuranceCompanies.Resource),

        new("View Diagnostic Categories",   ActionConstants.View,   DiagnosticCategories.Resource, IsBasic: true),
        new("Create Diagnostic Categories", ActionConstants.Create, DiagnosticCategories.Resource),
        new("Update Diagnostic Categories", ActionConstants.Update, DiagnosticCategories.Resource),
        new("Delete Diagnostic Categories", ActionConstants.Delete, DiagnosticCategories.Resource),

        new("View Custom Diagnostics",   ActionConstants.View,   CustomDiagnostics.Resource, IsBasic: true),
        new("Create Custom Diagnostics", ActionConstants.Create, CustomDiagnostics.Resource),
        new("Update Custom Diagnostics", ActionConstants.Update, CustomDiagnostics.Resource),
        new("Delete Custom Diagnostics", ActionConstants.Delete, CustomDiagnostics.Resource),

        new("View Procedure Categories",   ActionConstants.View,   ProcedureCategories.Resource, IsBasic: true),
        new("Create Procedure Categories", ActionConstants.Create, ProcedureCategories.Resource),
        new("Update Procedure Categories", ActionConstants.Update, ProcedureCategories.Resource),
        new("Delete Procedure Categories", ActionConstants.Delete, ProcedureCategories.Resource),

        new("View Procedure Codes",   ActionConstants.View,   ProcedureCodes.Resource, IsBasic: true),
        new("Create Procedure Codes", ActionConstants.Create, ProcedureCodes.Resource),
        new("Update Procedure Codes", ActionConstants.Update, ProcedureCodes.Resource),
        new("Delete Procedure Codes", ActionConstants.Delete, ProcedureCodes.Resource),

        new("View Races",   ActionConstants.View,   Races.Resource, IsBasic: true),
        new("Create Races", ActionConstants.Create, Races.Resource),
        new("Update Races", ActionConstants.Update, Races.Resource),
        new("Delete Races", ActionConstants.Delete, Races.Resource),

        new("View Ethnicities",   ActionConstants.View,   Ethnicities.Resource, IsBasic: true),
        new("Create Ethnicities", ActionConstants.Create, Ethnicities.Resource),
        new("Update Ethnicities", ActionConstants.Update, Ethnicities.Resource),
        new("Delete Ethnicities", ActionConstants.Delete, Ethnicities.Resource),

        new("View Languages",   ActionConstants.View,   Languages.Resource, IsBasic: true),
        new("Create Languages", ActionConstants.Create, Languages.Resource),
        new("Update Languages", ActionConstants.Update, Languages.Resource),
        new("Delete Languages", ActionConstants.Delete, Languages.Resource),

        new("View Smoking Statuses",   ActionConstants.View,   SmokingStatuses.Resource, IsBasic: true),
        new("Create Smoking Statuses", ActionConstants.Create, SmokingStatuses.Resource),
        new("Update Smoking Statuses", ActionConstants.Update, SmokingStatuses.Resource),
        new("Delete Smoking Statuses", ActionConstants.Delete, SmokingStatuses.Resource),

        new("View Preferred Contact Methods",   ActionConstants.View,   PreferredContactMethods.Resource, IsBasic: true),
        new("Create Preferred Contact Methods", ActionConstants.Create, PreferredContactMethods.Resource),
        new("Update Preferred Contact Methods", ActionConstants.Update, PreferredContactMethods.Resource),
        new("Delete Preferred Contact Methods", ActionConstants.Delete, PreferredContactMethods.Resource),

        new("View Referral Types",   ActionConstants.View,   ReferralTypes.Resource, IsBasic: true),
        new("Create Referral Types", ActionConstants.Create, ReferralTypes.Resource),
        new("Update Referral Types", ActionConstants.Update, ReferralTypes.Resource),
        new("Delete Referral Types", ActionConstants.Delete, ReferralTypes.Resource),
    ];
}
