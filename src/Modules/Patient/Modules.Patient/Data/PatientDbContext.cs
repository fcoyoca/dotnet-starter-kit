using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Patient.Data;

public sealed class PatientDbContext : BaseDbContext
{
    public const string Schema = "patient";

    public PatientDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PatientDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<Domain.Patient> Patients => Set<Domain.Patient>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PatientDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
