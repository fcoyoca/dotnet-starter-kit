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

    public static class CodeSources
    {
        public const string Resource = "Administration.CodeSources";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class IncidentTypes
    {
        public const string Resource = "Administration.IncidentTypes";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class PatientDocumentTypes
    {
        public const string Resource = "Administration.PatientDocumentTypes";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class Macros
    {
        public const string Resource = "Administration.Macros";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class EmailSettings
    {
        public const string Resource = "Administration.EmailSettings";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Update = $"Permissions.{Resource}.Update";
    }

    /// <summary>Report types and fields (the catalog behind the Macros admin).</summary>
    public static class ReportTemplates
    {
        public const string Resource = "Administration.ReportTemplates";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    public static class AppointmentTypes
    {
        public const string Resource = "Administration.AppointmentTypes";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Create = $"Permissions.{Resource}.Create";
        public const string Update = $"Permissions.{Resource}.Update";
        public const string Delete = $"Permissions.{Resource}.Delete";
    }

    /// <summary>Per-clinic schedule units (time interval + day start/end).</summary>
    public static class ScheduleConfig
    {
        public const string Resource = "Administration.ScheduleConfig";
        public const string View   = $"Permissions.{Resource}.View";
        public const string Update = $"Permissions.{Resource}.Update";
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

        new("View Code Sources",   ActionConstants.View,   CodeSources.Resource, IsBasic: true),
        new("Create Code Sources", ActionConstants.Create, CodeSources.Resource),
        new("Update Code Sources", ActionConstants.Update, CodeSources.Resource),
        new("Delete Code Sources", ActionConstants.Delete, CodeSources.Resource),

        new("View Incident Types",   ActionConstants.View,   IncidentTypes.Resource, IsBasic: true),
        new("Create Incident Types", ActionConstants.Create, IncidentTypes.Resource),
        new("Update Incident Types", ActionConstants.Update, IncidentTypes.Resource),
        new("Delete Incident Types", ActionConstants.Delete, IncidentTypes.Resource),

        new("View Patient Document Types",   ActionConstants.View,   PatientDocumentTypes.Resource, IsBasic: true),
        new("Create Patient Document Types", ActionConstants.Create, PatientDocumentTypes.Resource),
        new("Update Patient Document Types", ActionConstants.Update, PatientDocumentTypes.Resource),
        new("Delete Patient Document Types", ActionConstants.Delete, PatientDocumentTypes.Resource),

        new("View Macros",   ActionConstants.View,   Macros.Resource, IsBasic: true),
        new("Create Macros", ActionConstants.Create, Macros.Resource),
        new("Update Macros", ActionConstants.Update, Macros.Resource),
        new("Delete Macros", ActionConstants.Delete, Macros.Resource),

        new("View Email Settings",   ActionConstants.View,   EmailSettings.Resource, IsBasic: true),
        new("Update Email Settings", ActionConstants.Update, EmailSettings.Resource),

        new("View Report Templates",   ActionConstants.View,   ReportTemplates.Resource, IsBasic: true),
        new("Create Report Templates", ActionConstants.Create, ReportTemplates.Resource),
        new("Update Report Templates", ActionConstants.Update, ReportTemplates.Resource),
        new("Delete Report Templates", ActionConstants.Delete, ReportTemplates.Resource),

        new("View Appointment Types",   ActionConstants.View,   AppointmentTypes.Resource, IsBasic: true),
        new("Create Appointment Types", ActionConstants.Create, AppointmentTypes.Resource),
        new("Update Appointment Types", ActionConstants.Update, AppointmentTypes.Resource),
        new("Delete Appointment Types", ActionConstants.Delete, AppointmentTypes.Resource),

        new("View Schedule Config",   ActionConstants.View,   ScheduleConfig.Resource, IsBasic: true),
        new("Update Schedule Config", ActionConstants.Update, ScheduleConfig.Resource),

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
