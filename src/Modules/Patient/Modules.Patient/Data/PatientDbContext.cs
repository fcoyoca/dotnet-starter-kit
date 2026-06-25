using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Data.Configurations;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Patient.Data;

public sealed class PatientDbContext : BaseDbContext
{
    public const string Schema = "patient";

    private readonly IPhiEncryptor _phi;

    public PatientDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PatientDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment,
        IPhiEncryptor phi) : base(multiTenantContextAccessor, options, settings, environment)
    {
        ArgumentNullException.ThrowIfNull(phi);
        _phi = phi;
    }

    public DbSet<Domain.Patient> Patients => Set<Domain.Patient>();
    public DbSet<Domain.PatientIncident> PatientIncidents => Set<Domain.PatientIncident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        // PatientConfiguration requires IPhiEncryptor — apply it directly instead of via reflection
        modelBuilder.ApplyConfiguration(new PatientConfiguration(_phi));
        modelBuilder.ApplyConfiguration(new PatientIncidentConfiguration());
        modelBuilder.ApplyConfiguration(new PatientIncidentDiagnosticConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
