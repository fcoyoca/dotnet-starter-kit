using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Patient.Tests.Data;

/// <summary>
/// Regression cover for the PatientReport write path: children of the aggregate carry an app-assigned
/// <c>Guid.CreateVersion7()</c> key, so their configuration must declare <c>ValueGeneratedNever()</c>.
///
/// Without it EF's convention marks a Guid PK <c>ValueGenerated.OnAdd</c>, and a child discovered on an
/// ALREADY-TRACKED parent is resolved by "is the key set?" — a populated Guid reads as an existing row, so
/// EF tracks the brand-new child as <c>Modified</c> and emits <c>UPDATE … WHERE Id = &lt;new guid&gt;</c>
/// against a row that does not exist → 0 rows affected → <c>DbUpdateConcurrencyException</c> on save.
/// (Create was unaffected: <c>Add(report)</c> marks the whole graph Added.)
///
/// These assert entity STATE rather than collection contents — the earlier in-place-reconcile regression
/// tests in <c>Domain/PatientReportTests</c> are pure domain tests and pass either way, which is why the
/// exception survived that fix. Same defect previously fixed in Tickets/Catalog/Chat.
/// </summary>
public sealed class PatientReportTrackingTests
{
    [Fact]
    public void AddedFieldValue_On_TrackedReport_Should_Be_Added_Not_Modified()
    {
        #region Arrange
        using PatientDbContext ctx = NewContext();
        PatientReport report = NewReport();
        report.SetFieldValues([(10, "existing")]);
        ctx.Attach(report); // whole graph Unchanged — exactly like a report loaded for edit
        #endregion

        #region Act
        report.SetFieldValues([(10, "edited"), (11, "brand new")]);
        ctx.ChangeTracker.DetectChanges();
        #endregion

        #region Assert
        StateOf(ctx, (PatientReportFieldValue f) => f.ReportFieldId == 11)
            .ShouldBe(EntityState.Added, "a field value that has no row yet must INSERT, not UPDATE-0-rows");
        StateOf(ctx, (PatientReportFieldValue f) => f.ReportFieldId == 10)
            .ShouldBe(EntityState.Modified, "a field value reconciled in place keeps its row and must UPDATE");
        #endregion
    }

    [Fact]
    public void AddedAddendum_On_TrackedReport_Should_Be_Added_Not_Modified()
    {
        #region Arrange
        using PatientDbContext ctx = NewContext();
        PatientReport report = NewReport();
        ctx.Attach(report);
        #endregion

        #region Act
        report.AddAddendum("user-1", "Dr Who", "Follow-up note.");
        ctx.ChangeTracker.DetectChanges();
        #endregion

        #region Assert
        StateOf(ctx, (PatientReportAddendum a) => a.Text == "Follow-up note.").ShouldBe(EntityState.Added);
        #endregion
    }

    [Fact]
    public void AddedAssociatedProblem_On_TrackedReport_Should_Be_Added_Not_Modified()
    {
        #region Arrange
        using PatientDbContext ctx = NewContext();
        PatientReport report = NewReport();
        ctx.Attach(report);
        Guid problemId = Guid.CreateVersion7();
        #endregion

        #region Act
        report.SetAssociatedProblems([problemId]);
        ctx.ChangeTracker.DetectChanges();
        #endregion

        #region Assert
        StateOf(ctx, (PatientReportProblem p) => p.ProblemId == problemId).ShouldBe(EntityState.Added);
        #endregion
    }

    /// <summary>
    /// Guards the whole module: any single-Guid PK left on the EF <c>OnAdd</c> convention while the domain
    /// assigns the key itself is the same latent bug, so a new aggregate child cannot reintroduce it silently.
    /// Aggregate roots are exempt — they are persisted via <c>Add(root)</c>, which states the graph explicitly.
    /// </summary>
    [Fact]
    public void AppAssigned_Guid_Keys_Of_Aggregate_Children_Should_Be_ValueGeneratedNever()
    {
        #region Arrange
        using PatientDbContext ctx = NewContext();

        // Every type reachable only as a nav-collection child of an aggregate root.
        HashSet<Type> children =
        [
            typeof(PatientReportFieldValue),
            typeof(PatientReportAddendum),
            typeof(PatientReportProblem),
            typeof(PatientIncidentDiagnostic),
            typeof(SuperBillProcedure),
            typeof(SuperBillProcedureDiagnostic),
        ];
        #endregion

        #region Act
        List<string> offenders = ctx.Model.GetEntityTypes()
            .Where(e => !e.IsOwned() && children.Contains(e.ClrType))
            .Select(e => new { e.ClrType.Name, Key = e.FindPrimaryKey() })
            .Where(x => x.Key is not null
                && x.Key.Properties.Count == 1
                && x.Key.Properties[0].ClrType == typeof(Guid)
                && x.Key.Properties[0].ValueGenerated == ValueGenerated.OnAdd)
            .Select(x => x.Name)
            .ToList();
        #endregion

        #region Assert
        offenders.ShouldBeEmpty(
            "aggregate children with app-assigned Guid keys must declare ValueGeneratedNever() or EF tracks " +
            "a new child as Modified → UPDATE-0-rows → DbUpdateConcurrencyException");
        #endregion
    }

    private static EntityState StateOf<T>(PatientDbContext ctx, Func<T, bool> match) where T : class =>
        ctx.ChangeTracker.Entries<T>().Single(e => match(e.Entity)).State;

    private static PatientReport NewReport() => PatientReport.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), 1,
        new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), null, null, isNoShow: false);

    /// <summary>
    /// Real model on the real provider; change tracking never opens a connection, so this needs no database.
    /// Mirrors Architecture.Tests' TenantIsolationTests context construction.
    /// </summary>
    private static PatientDbContext NewContext()
    {
        var builder = new DbContextOptionsBuilder<PatientDbContext>();
        builder.UseNpgsql("Host=tracking;Database=tracking;Username=tracking;Password=tracking");

        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });

        return new PatientDbContext(new StubAccessor(), builder.Options, settings, new StubEnvironment());
    }

    private sealed class StubAccessor : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext<AppTenantInfo> MultiTenantContext { get; set; } =
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("tracking", "tracking", string.Empty, "t@t", "tracking"));

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => MultiTenantContext;
    }

    private sealed class StubEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "tracking";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
