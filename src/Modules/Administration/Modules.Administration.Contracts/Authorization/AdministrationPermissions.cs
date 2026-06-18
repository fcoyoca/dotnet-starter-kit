using FSH.Framework.Shared.Constants;

namespace FSH.Modules.Administration.Contracts.Authorization;

public static class AdministrationPermissions
{
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
