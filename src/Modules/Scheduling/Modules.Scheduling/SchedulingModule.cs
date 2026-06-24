using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Scheduling.SchedulingModule), 810)]

namespace FSH.Modules.Scheduling;

public sealed class SchedulingModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(SchedulingPermissions.All);

        builder.Services.AddHeroDbContext<SchedulingDbContext>();
        builder.Services.AddScoped<IDbInitializer, SchedulingDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<SchedulingDbContext>(
                name: "db:scheduling",
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

        endpoints
            .MapGroup("api/v{version:apiVersion}/scheduling")
            .WithTags("Scheduling")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();
    }
}
