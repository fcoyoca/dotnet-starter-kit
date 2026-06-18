using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Administration.Contracts.Authorization;
using FSH.Modules.Administration.Data;
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
    }
}
