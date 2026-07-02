using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Features.v1.Clinics.CreateClinic;
using FSH.Modules.Administration.Features.v1.Clinics.DeleteClinic;
using FSH.Modules.Administration.Features.v1.Clinics.GetClinicById;
using FSH.Modules.Administration.Features.v1.Clinics.ListClinics;
using FSH.Modules.Administration.Features.v1.Clinics.UpdateClinic;
using FSH.Modules.Administration.Features.v1.Departments.CreateDepartment;
using FSH.Modules.Administration.Features.v1.Departments.DeleteDepartment;
using FSH.Modules.Administration.Features.v1.Departments.GetDepartmentById;
using FSH.Modules.Administration.Features.v1.Departments.ListDepartments;
using FSH.Modules.Administration.Features.v1.Departments.UpdateDepartment;
using FSH.Modules.Administration.Features.v1.Ethnicities.CreateEthnicity;
using FSH.Modules.Administration.Features.v1.Ethnicities.DeleteEthnicity;
using FSH.Modules.Administration.Features.v1.Ethnicities.GetEthnicityById;
using FSH.Modules.Administration.Features.v1.Ethnicities.ListEthnicities;
using FSH.Modules.Administration.Features.v1.Ethnicities.UpdateEthnicity;
using FSH.Modules.Administration.Features.v1.Languages.CreateLanguage;
using FSH.Modules.Administration.Features.v1.Languages.DeleteLanguage;
using FSH.Modules.Administration.Features.v1.Languages.GetLanguageById;
using FSH.Modules.Administration.Features.v1.Languages.ListLanguages;
using FSH.Modules.Administration.Features.v1.Languages.UpdateLanguage;
using FSH.Modules.Administration.Features.v1.ProcedureCodes.CreateProcedureCode;
using FSH.Modules.Administration.Features.v1.ProcedureCodes.DeleteProcedureCode;
using FSH.Modules.Administration.Features.v1.ProcedureCodes.GetProcedureCodeById;
using FSH.Modules.Administration.Features.v1.ProcedureCodes.ListProcedureCodes;
using FSH.Modules.Administration.Features.v1.ProcedureCodes.UpdateProcedureCode;
using FSH.Modules.Administration.Features.v1.ProcedureCategories.CreateProcedureCategory;
using FSH.Modules.Administration.Features.v1.ProcedureCategories.DeleteProcedureCategory;
using FSH.Modules.Administration.Features.v1.ProcedureCategories.GetProcedureCategoryById;
using FSH.Modules.Administration.Features.v1.ProcedureCategories.ListProcedureCategories;
using FSH.Modules.Administration.Features.v1.ProcedureCategories.UpdateProcedureCategory;
using FSH.Modules.Administration.Features.v1.Providers.ClearProviderSignature;
using FSH.Modules.Administration.Features.v1.Providers.CreateProvider;
using FSH.Modules.Administration.Features.v1.Providers.DeleteProvider;
using FSH.Modules.Administration.Features.v1.Providers.GetProviderById;
using FSH.Modules.Administration.Features.v1.Providers.ListProviders;
using FSH.Modules.Administration.Features.v1.Providers.SetProviderSignature;
using FSH.Modules.Administration.Features.v1.Providers.UpdateProvider;
using FSH.Modules.Administration.Features.v1.InsuranceTypes.CreateInsuranceType;
using FSH.Modules.Administration.Features.v1.InsuranceTypes.DeleteInsuranceType;
using FSH.Modules.Administration.Features.v1.InsuranceTypes.GetInsuranceTypeById;
using FSH.Modules.Administration.Features.v1.InsuranceTypes.ListInsuranceTypes;
using FSH.Modules.Administration.Features.v1.InsuranceTypes.UpdateInsuranceType;
using FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.ListInsuranceTypeProcedures;
using FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.SetProcedures;
using FSH.Modules.Administration.Features.v1.CodeSources.CreateCodeSource;
using FSH.Modules.Administration.Features.v1.CodeSources.DeleteCodeSource;
using FSH.Modules.Administration.Features.v1.CodeSources.GetCodeSourceById;
using FSH.Modules.Administration.Features.v1.CodeSources.ListCodeSources;
using FSH.Modules.Administration.Features.v1.CodeSources.UpdateCodeSource;
using FSH.Modules.Administration.Features.v1.IncidentTypes.CreateIncidentType;
using FSH.Modules.Administration.Features.v1.IncidentTypes.DeleteIncidentType;
using FSH.Modules.Administration.Features.v1.IncidentTypes.GetIncidentTypeById;
using FSH.Modules.Administration.Features.v1.IncidentTypes.ListIncidentTypes;
using FSH.Modules.Administration.Features.v1.IncidentTypes.UpdateIncidentType;
using FSH.Modules.Administration.Features.v1.PatientDocumentTypes.CreatePatientDocumentType;
using FSH.Modules.Administration.Features.v1.PatientDocumentTypes.DeletePatientDocumentType;
using FSH.Modules.Administration.Features.v1.PatientDocumentTypes.GetPatientDocumentTypeById;
using FSH.Modules.Administration.Features.v1.PatientDocumentTypes.ListPatientDocumentTypes;
using FSH.Modules.Administration.Features.v1.PatientDocumentTypes.UpdatePatientDocumentType;
using FSH.Modules.Administration.Features.v1.Macros.CreateMacro;
using FSH.Modules.Administration.Features.v1.Macros.DeleteMacro;
using FSH.Modules.Administration.Features.v1.Macros.GetMacroById;
using FSH.Modules.Administration.Features.v1.Macros.ListMacros;
using FSH.Modules.Administration.Features.v1.Macros.UpdateMacro;
using FSH.Modules.Administration.Features.v1.EmailSettings.GetEmailSettings;
using FSH.Modules.Administration.Features.v1.EmailSettings.UpdateEmailSettings;
using FSH.Modules.Administration.Features.v1.ReportTemplates.ListReportTypes;
using FSH.Modules.Administration.Features.v1.ReportTemplates.ListReportFields;
using FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportType;
using FSH.Modules.Administration.Features.v1.ReportTemplates.UpdateReportType;
using FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportType;
using FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportField;
using FSH.Modules.Administration.Features.v1.ReportTemplates.UpdateReportField;
using FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportField;
using FSH.Modules.Administration.Features.v1.AppointmentTypes.ListAppointmentTypes;
using FSH.Modules.Administration.Features.v1.AppointmentTypes.CreateAppointmentType;
using FSH.Modules.Administration.Features.v1.AppointmentTypes.UpdateAppointmentType;
using FSH.Modules.Administration.Features.v1.AppointmentTypes.DeleteAppointmentType;
using FSH.Modules.Administration.Features.v1.ScheduleConfig.GetScheduleConfig;
using FSH.Modules.Administration.Features.v1.ScheduleConfig.UpsertScheduleConfig;
using FSH.Modules.Administration.Features.v1.InsuranceCompanies.CreateInsuranceCompany;
using FSH.Modules.Administration.Features.v1.InsuranceCompanies.DeleteInsuranceCompany;
using FSH.Modules.Administration.Features.v1.InsuranceCompanies.GetInsuranceCompanyById;
using FSH.Modules.Administration.Features.v1.InsuranceCompanies.ListInsuranceCompanies;
using FSH.Modules.Administration.Features.v1.InsuranceCompanies.UpdateInsuranceCompany;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.CreateCustomDiagnostic;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.DeleteCustomDiagnostic;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.GetCustomDiagnosticById;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.ListCustomDiagnostics;
using FSH.Modules.Administration.Features.v1.CustomDiagnostics.UpdateCustomDiagnostic;
using FSH.Modules.Administration.Features.v1.Diagnostics.CreateDiagnostic;
using FSH.Modules.Administration.Features.v1.Diagnostics.DeleteDiagnostic;
using FSH.Modules.Administration.Features.v1.Diagnostics.GetDiagnosticById;
using FSH.Modules.Administration.Features.v1.Diagnostics.ListDiagnostics;
using FSH.Modules.Administration.Features.v1.Diagnostics.UpdateDiagnostic;
using FSH.Modules.Administration.Features.v1.Drugs.CreateDrug;
using FSH.Modules.Administration.Features.v1.Drugs.DeleteDrug;
using FSH.Modules.Administration.Features.v1.Drugs.GetDrugById;
using FSH.Modules.Administration.Features.v1.Drugs.ListDrugs;
using FSH.Modules.Administration.Features.v1.Drugs.UpdateDrug;
using FSH.Modules.Administration.Features.v1.DiagnosticCategories.CreateDiagnosticCategory;
using FSH.Modules.Administration.Features.v1.DiagnosticCategories.DeleteDiagnosticCategory;
using FSH.Modules.Administration.Features.v1.DiagnosticCategories.GetDiagnosticCategoryById;
using FSH.Modules.Administration.Features.v1.DiagnosticCategories.ListDiagnosticCategories;
using FSH.Modules.Administration.Features.v1.DiagnosticCategories.UpdateDiagnosticCategory;
using FSH.Modules.Administration.Features.v1.PreferredContactMethods.CreatePreferredContactMethod;
using FSH.Modules.Administration.Features.v1.PreferredContactMethods.DeletePreferredContactMethod;
using FSH.Modules.Administration.Features.v1.PreferredContactMethods.GetPreferredContactMethodById;
using FSH.Modules.Administration.Features.v1.PreferredContactMethods.ListPreferredContactMethods;
using FSH.Modules.Administration.Features.v1.PreferredContactMethods.UpdatePreferredContactMethod;
using FSH.Modules.Administration.Features.v1.Races.CreateRace;
using FSH.Modules.Administration.Features.v1.Races.DeleteRace;
using FSH.Modules.Administration.Features.v1.Races.GetRaceById;
using FSH.Modules.Administration.Features.v1.Races.ListRaces;
using FSH.Modules.Administration.Features.v1.Races.UpdateRace;
using FSH.Modules.Administration.Features.v1.ReferralTypes.CreateReferralType;
using FSH.Modules.Administration.Features.v1.ReferralTypes.DeleteReferralType;
using FSH.Modules.Administration.Features.v1.ReferralTypes.GetReferralTypeById;
using FSH.Modules.Administration.Features.v1.ReferralTypes.ListReferralTypes;
using FSH.Modules.Administration.Features.v1.ReferralTypes.UpdateReferralType;
using FSH.Modules.Administration.Features.v1.SmokingStatuses.CreateSmokingStatus;
using FSH.Modules.Administration.Features.v1.SmokingStatuses.DeleteSmokingStatus;
using FSH.Modules.Administration.Features.v1.SmokingStatuses.GetSmokingStatusById;
using FSH.Modules.Administration.Features.v1.SmokingStatuses.ListSmokingStatuses;
using FSH.Modules.Administration.Features.v1.SmokingStatuses.UpdateSmokingStatus;
using FSH.Modules.Administration.Features.v1.AllergyReactions.CreateAllergyReaction;
using FSH.Modules.Administration.Features.v1.AllergyReactions.DeleteAllergyReaction;
using FSH.Modules.Administration.Features.v1.AllergyReactions.GetAllergyReactionById;
using FSH.Modules.Administration.Features.v1.AllergyReactions.ListAllergyReactions;
using FSH.Modules.Administration.Features.v1.AllergyReactions.UpdateAllergyReaction;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.CreateMedicationDoseUnit;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.DeleteMedicationDoseUnit;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.GetMedicationDoseUnitById;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.ListMedicationDoseUnits;
using FSH.Modules.Administration.Features.v1.MedicationDoseUnits.UpdateMedicationDoseUnit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Administration.AdministrationModule), 800)]

namespace FSH.Modules.Administration;

public sealed class AdministrationModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(AdministrationPermissions.All);

        builder.Services.AddHeroDbContext<AdministrationDbContext>();
        builder.Services.AddScoped<IDbInitializer, AdministrationDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<AdministrationDbContext>(
                name: "db:administration",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/administration")
            .WithTags("Administration")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapCreateClinicEndpoint();
        group.MapListClinicsEndpoint();
        group.MapGetClinicByIdEndpoint();
        group.MapUpdateClinicEndpoint();
        group.MapDeleteClinicEndpoint();

        group.MapCreateDepartmentEndpoint();
        group.MapListDepartmentsEndpoint();
        group.MapGetDepartmentByIdEndpoint();
        group.MapUpdateDepartmentEndpoint();
        group.MapDeleteDepartmentEndpoint();

        group.MapCreateProviderEndpoint();
        group.MapListProvidersEndpoint();
        group.MapGetProviderByIdEndpoint();
        group.MapUpdateProviderEndpoint();
        group.MapSetProviderSignatureEndpoint();
        group.MapClearProviderSignatureEndpoint();
        group.MapDeleteProviderEndpoint();

        group.MapCreateInsuranceTypeEndpoint();
        group.MapListInsuranceTypesEndpoint();
        group.MapGetInsuranceTypeByIdEndpoint();
        group.MapUpdateInsuranceTypeEndpoint();
        group.MapDeleteInsuranceTypeEndpoint();

        group.MapListInsuranceTypeProceduresEndpoint();
        group.MapSetInsuranceTypeProceduresEndpoint();

        group.MapCreateInsuranceCompanyEndpoint();
        group.MapListInsuranceCompaniesEndpoint();
        group.MapGetInsuranceCompanyByIdEndpoint();
        group.MapUpdateInsuranceCompanyEndpoint();
        group.MapDeleteInsuranceCompanyEndpoint();

        group.MapCreateDiagnosticCategoryEndpoint();
        group.MapListDiagnosticCategoriesEndpoint();
        group.MapGetDiagnosticCategoryByIdEndpoint();
        group.MapUpdateDiagnosticCategoryEndpoint();
        group.MapDeleteDiagnosticCategoryEndpoint();

        group.MapCreateCustomDiagnosticEndpoint();
        group.MapListCustomDiagnosticsEndpoint();
        group.MapGetCustomDiagnosticByIdEndpoint();
        group.MapUpdateCustomDiagnosticEndpoint();
        group.MapDeleteCustomDiagnosticEndpoint();

        group.MapCreateDiagnosticEndpoint();
        group.MapListDiagnosticsEndpoint();
        group.MapGetDiagnosticByIdEndpoint();
        group.MapUpdateDiagnosticEndpoint();
        group.MapDeleteDiagnosticEndpoint();

        group.MapListDrugsEndpoint();
        group.MapGetDrugByIdEndpoint();
        group.MapCreateDrugEndpoint();
        group.MapUpdateDrugEndpoint();
        group.MapDeleteDrugEndpoint();

        group.MapCreateProcedureCategoryEndpoint();
        group.MapListProcedureCategoriesEndpoint();
        group.MapGetProcedureCategoryByIdEndpoint();
        group.MapUpdateProcedureCategoryEndpoint();
        group.MapDeleteProcedureCategoryEndpoint();

        group.MapCreateProcedureCodeEndpoint();
        group.MapCreateCodeSourceEndpoint();
        group.MapListCodeSourcesEndpoint();
        group.MapGetCodeSourceByIdEndpoint();
        group.MapUpdateCodeSourceEndpoint();
        group.MapDeleteCodeSourceEndpoint();

        group.MapCreateIncidentTypeEndpoint();
        group.MapListIncidentTypesEndpoint();
        group.MapGetIncidentTypeByIdEndpoint();
        group.MapUpdateIncidentTypeEndpoint();
        group.MapDeleteIncidentTypeEndpoint();

        group.MapCreatePatientDocumentTypeEndpoint();
        group.MapListPatientDocumentTypesEndpoint();
        group.MapGetPatientDocumentTypeByIdEndpoint();
        group.MapUpdatePatientDocumentTypeEndpoint();
        group.MapDeletePatientDocumentTypeEndpoint();

        group.MapCreateMacroEndpoint();
        group.MapListMacrosEndpoint();
        group.MapGetMacroByIdEndpoint();
        group.MapUpdateMacroEndpoint();
        group.MapDeleteMacroEndpoint();

        group.MapGetEmailSettingsEndpoint();
        group.MapUpdateEmailSettingsEndpoint();

        group.MapListReportTypesEndpoint();
        group.MapListReportFieldsEndpoint();
        group.MapCreateReportTypeEndpoint();
        group.MapUpdateReportTypeEndpoint();
        group.MapDeleteReportTypeEndpoint();
        group.MapCreateReportFieldEndpoint();
        group.MapUpdateReportFieldEndpoint();
        group.MapDeleteReportFieldEndpoint();

        group.MapListAppointmentTypesEndpoint();
        group.MapCreateAppointmentTypeEndpoint();
        group.MapUpdateAppointmentTypeEndpoint();
        group.MapDeleteAppointmentTypeEndpoint();

        group.MapGetScheduleConfigEndpoint();
        group.MapUpsertScheduleConfigEndpoint();

        group.MapListProcedureCodesEndpoint();
        group.MapGetProcedureCodeByIdEndpoint();
        group.MapUpdateProcedureCodeEndpoint();
        group.MapDeleteProcedureCodeEndpoint();

        group.MapCreateRaceEndpoint();
        group.MapListRacesEndpoint();
        group.MapGetRaceByIdEndpoint();
        group.MapUpdateRaceEndpoint();
        group.MapDeleteRaceEndpoint();

        group.MapCreateEthnicityEndpoint();
        group.MapListEthnicitiesEndpoint();
        group.MapGetEthnicityByIdEndpoint();
        group.MapUpdateEthnicityEndpoint();
        group.MapDeleteEthnicityEndpoint();

        group.MapCreateLanguageEndpoint();
        group.MapListLanguagesEndpoint();
        group.MapGetLanguageByIdEndpoint();
        group.MapUpdateLanguageEndpoint();
        group.MapDeleteLanguageEndpoint();

        group.MapCreateSmokingStatusEndpoint();
        group.MapListSmokingStatusesEndpoint();
        group.MapGetSmokingStatusByIdEndpoint();
        group.MapUpdateSmokingStatusEndpoint();
        group.MapDeleteSmokingStatusEndpoint();

        group.MapCreatePreferredContactMethodEndpoint();
        group.MapListPreferredContactMethodsEndpoint();
        group.MapGetPreferredContactMethodByIdEndpoint();
        group.MapUpdatePreferredContactMethodEndpoint();
        group.MapDeletePreferredContactMethodEndpoint();

        group.MapCreateReferralTypeEndpoint();
        group.MapListReferralTypesEndpoint();
        group.MapGetReferralTypeByIdEndpoint();
        group.MapUpdateReferralTypeEndpoint();
        group.MapDeleteReferralTypeEndpoint();

        group.MapCreateAllergyReactionEndpoint();
        group.MapListAllergyReactionsEndpoint();
        group.MapGetAllergyReactionByIdEndpoint();
        group.MapUpdateAllergyReactionEndpoint();
        group.MapDeleteAllergyReactionEndpoint();

        group.MapCreateMedicationDoseUnitEndpoint();
        group.MapListMedicationDoseUnitsEndpoint();
        group.MapGetMedicationDoseUnitByIdEndpoint();
        group.MapUpdateMedicationDoseUnitEndpoint();
        group.MapDeleteMedicationDoseUnitEndpoint();
    }
}
