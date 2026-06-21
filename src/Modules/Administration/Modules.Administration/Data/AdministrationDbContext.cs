using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Administration.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Administration.Data;

public sealed class AdministrationDbContext : BaseDbContext
{
    public const string Schema = "administration";

    public AdministrationDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<AdministrationDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<InsuranceType> InsuranceTypes => Set<InsuranceType>();
    public DbSet<InsuranceCompany> InsuranceCompanies => Set<InsuranceCompany>();
    public DbSet<DiagnosticCategory> DiagnosticCategories => Set<DiagnosticCategory>();
    public DbSet<CustomDiagnostic> CustomDiagnostics => Set<CustomDiagnostic>();
    public DbSet<ProcedureCategory> ProcedureCategories => Set<ProcedureCategory>();
    public DbSet<Race> Races => Set<Race>();
    public DbSet<Ethnicity> Ethnicities => Set<Ethnicity>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<SmokingStatus> SmokingStatuses => Set<SmokingStatus>();
    public DbSet<PreferredContactMethod> PreferredContactMethods => Set<PreferredContactMethod>();
    public DbSet<ReferralType> ReferralTypes => Set<ReferralType>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministrationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
