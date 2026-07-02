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

    /// <summary>
    /// Canonical <see cref="BaseDbContext"/> constructor (no <see cref="IPhiEncryptor"/>),
    /// present so design-time tooling and <c>Architecture.Tests</c>'
    /// <c>TenantIsolationTests</c> can construct the context to inspect its model.
    /// Production never selects this overload: the .NET DI container greedily binds
    /// the 5-arg constructor above (a strict superset whose every parameter is
    /// registered), so the real encryptor is always used. The no-op encryptor here
    /// only ever participates in model-metadata inspection, never PHI read/write.
    /// </summary>
    public PatientDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PatientDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment)
        : this(multiTenantContextAccessor, options, settings, environment, NoOpPhiEncryptor.Instance)
    {
    }

    /// <summary>Identity encryptor used only by the design-time/test constructor above.</summary>
    private sealed class NoOpPhiEncryptor : IPhiEncryptor
    {
        public static readonly NoOpPhiEncryptor Instance = new();
        public string? Encrypt(string? plaintext) => plaintext;
        public string? Decrypt(string? ciphertext) => ciphertext;
        public string? HashForSearch(string? value) => value;
    }

    public DbSet<Domain.Patient> Patients => Set<Domain.Patient>();
    public DbSet<Domain.PatientIncident> PatientIncidents => Set<Domain.PatientIncident>();
    public DbSet<Domain.PatientReport> PatientReports => Set<Domain.PatientReport>();
    public DbSet<Domain.PatientProblem> PatientProblems => Set<Domain.PatientProblem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.HasSequence<long>("PatientCodeSequence", schema: Schema)
            .StartsAt(100_000)
            .IncrementsBy(1);
        // PatientConfiguration requires IPhiEncryptor — apply it directly instead of via reflection
        modelBuilder.ApplyConfiguration(new PatientConfiguration(_phi));
        modelBuilder.ApplyConfiguration(new PatientIncidentConfiguration());
        modelBuilder.ApplyConfiguration(new PatientIncidentDiagnosticConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportFieldValueConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportAddendumConfiguration());
        modelBuilder.ApplyConfiguration(new PatientReportProblemConfiguration());
        modelBuilder.ApplyConfiguration(new PatientProblemConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
