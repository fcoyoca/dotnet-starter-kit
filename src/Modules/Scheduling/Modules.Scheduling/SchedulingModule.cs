using Asp.Versioning;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.Scheduling.Contracts.Authorization;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using FSH.Modules.Scheduling.Features.v1.Appointments.CancelAppointment;
using FSH.Modules.Scheduling.Features.v1.Appointments.CheckInAppointment;
using FSH.Modules.Scheduling.Features.v1.Appointments.CheckOutAppointment;
using FSH.Modules.Scheduling.Features.v1.Appointments.CreateAppointment;
using FSH.Modules.Scheduling.Features.v1.Appointments.DeleteAppointment;
using FSH.Modules.Scheduling.Features.v1.Appointments.GetAppointmentById;
using FSH.Modules.Scheduling.Features.v1.Appointments.ListAppointments;
using FSH.Modules.Scheduling.Features.v1.Appointments.ListPatientAppointments;
using FSH.Modules.Scheduling.Features.v1.Appointments.NoShowAppointment;
using FSH.Modules.Scheduling.Features.v1.Appointments.UpdateAppointment;
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
        builder.Services.AddScoped<AppointmentRealtimeNotifier>();

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

        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/scheduling")
            .WithTags("Scheduling")
            .WithApiVersionSet(versionSet)
            .RequireAuthorization();

        group.MapListAppointmentsEndpoint();
        group.MapListPatientAppointmentsEndpoint();
        group.MapGetAppointmentByIdEndpoint();
        group.MapCreateAppointmentEndpoint();
        group.MapUpdateAppointmentEndpoint();
        group.MapDeleteAppointmentEndpoint();
        group.MapCheckInAppointmentEndpoint();
        group.MapCheckOutAppointmentEndpoint();
        group.MapCancelAppointmentEndpoint();
        group.MapNoShowAppointmentEndpoint();
    }
}
