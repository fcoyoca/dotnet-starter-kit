using Asp.Versioning;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Web.Modules;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Features.v1.Claims.GetClaimById;
using FSH.Modules.Claims.Features.v1.Claims.GetClaims;
using FSH.Modules.Claims.Features.v1.Claims.MarkDenied;
using FSH.Modules.Claims.Features.v1.Claims.MarkPaid;
using FSH.Modules.Claims.Features.v1.Claims.MarkReady;
using FSH.Modules.Claims.Features.v1.Claims.Submit;
using FSH.Modules.Claims.Features.v1.Claims.Void;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Claims.ClaimsModule), 850)]

namespace FSH.Modules.Claims;

public sealed class ClaimsModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        FSH.Framework.Shared.Constants.PermissionConstants.Register(
            FSH.Modules.Claims.Contracts.Authorization.ClaimsPermissions.All);

        builder.Services.AddHeroDbContext<ClaimsDbContext>();
        builder.Services.AddScoped<IDbInitializer, ClaimsDbInitializer>();

        // Consume SuperBillSavedIntegrationEvent (Patient.Contracts) + register the stub submitter.
        builder.Services.AddIntegrationEventHandlers(typeof(ClaimsModule).Assembly);
        builder.Services.AddScoped<Submission.IClaimSubmitter, Submission.StubClaimSubmitter>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<ClaimsDbContext>(name: "db:claims", failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { /* none */ }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/claims")
            .WithTags("Claims")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapGetClaimsEndpoint();
        group.MapGetClaimByIdEndpoint();
        group.MapMarkClaimReadyEndpoint();
        group.MapSubmitClaimEndpoint();
        group.MapMarkClaimPaidEndpoint();
        group.MapMarkClaimDeniedEndpoint();
        group.MapVoidClaimEndpoint();
    }
}
