using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Claims.Data;
using FSH.Modules.Claims.Domain;
using FSH.Modules.Claims.Features.v1.Claims.SuperBillSaved;
using FSH.Modules.Patient.Contracts.Events;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Claims.Tests.Features;

public sealed class SuperBillSavedIntegrationEventHandlerTests
{
    private const string TestTenant = "test";

    // Real Finbuckle setter+accessor pair over a single shared backing field: the setter the handler
    // calls and the accessor the ClaimsDbContext reads its query filter from are the SAME context, so a
    // tenant the handler installs is actually observed by the DbContext (mirrors WebhookFanoutHandlerTests).
    private sealed class TestTenantAccessor : IMultiTenantContextAccessor<AppTenantInfo>, IMultiTenantContextSetter
    {
        private IMultiTenantContext<AppTenantInfo> _context = new MultiTenantContext<AppTenantInfo>(new AppTenantInfo());

        public IMultiTenantContext<AppTenantInfo> MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => _context;

        IMultiTenantContext IMultiTenantContextSetter.MultiTenantContext
        {
            set => _context = (IMultiTenantContext<AppTenantInfo>)value;
        }

        public void SetTenant(string tenantId) =>
            _context = new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(tenantId, tenantId));
    }

    private static ClaimsDbContext CreateContext(
        string dbName, IMultiTenantContextAccessor<AppTenantInfo> accessor, InMemoryDatabaseRoot? root = null)
    {
        // A shared InMemoryDatabaseRoot lets independently-constructed contexts see the same store — needed
        // when a test writes under one context and re-reads under a fresh, differently-tenanted context
        // (Finbuckle 10 captures the query-filter tenant per-context, so isolation must be probed with a new
        // context per tenant). Without a shared root each UseInMemoryDatabase spins up its own isolated store.
        var builder = new DbContextOptionsBuilder<ClaimsDbContext>();
        var options = (root is null
                ? builder.UseInMemoryDatabase(dbName)
                : builder.UseInMemoryDatabase(dbName, root))
            .Options;
        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql", ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });
        return new ClaimsDbContext(accessor, options, settings, Substitute.For<IHostEnvironment>());
    }

    private static SuperBillSavedIntegrationEventHandler CreateHandler(ClaimsDbContext db) =>
        new(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);

    private static SuperBillSavedIntegrationEvent Event(
        Guid superBillId, Guid? insuranceTypeId, params ReportProcedureItem[] procedures) =>
        EventForTenant(superBillId, TestTenant, insuranceTypeId, procedures);

    private static SuperBillSavedIntegrationEvent EventForTenant(
        Guid superBillId, string? tenantId, Guid? insuranceTypeId, params ReportProcedureItem[] procedures) =>
        new(
            Id: Guid.NewGuid(), OccurredOnUtc: DateTime.UtcNow, TenantId: tenantId,
            CorrelationId: Guid.NewGuid().ToString(), Source: "Patient",
            SuperBillId: superBillId, ReportId: Guid.NewGuid(), PatientId: Guid.NewGuid(),
            IsBilled: false, Procedures: procedures, InsuranceTypeId: insuranceTypeId);

    private static ReportProcedureItem Proc(string code, decimal charge) =>
        new(ProcedureCodeId: Guid.NewGuid(), Code: code, Description: code, Charge: charge, DiagnosticIds: []);

    [Fact]
    public async Task Creates_Draft_Claim_Snapshotting_Lines_And_Total()
    {
        var tenant = new TestTenantAccessor();
        tenant.SetTenant(TestTenant);
        using var db = CreateContext(Guid.NewGuid().ToString(), tenant);
        var sut = CreateHandler(db);
        var sb = Guid.NewGuid();

        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("99213", 120m), Proc("93000", 150m)), CancellationToken.None);

        var claim = await db.Claims.Include(c => c.Lines).SingleAsync();
        claim.SuperBillId.ShouldBe(sb);
        claim.Status.ShouldBe(ClaimStatus.Draft);
        claim.TotalCharge.ShouldBe(270m);
        claim.Lines.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Resave_While_Draft_Refreshes_Snapshot_In_Place()
    {
        var tenant = new TestTenantAccessor();
        tenant.SetTenant(TestTenant);
        using var db = CreateContext(Guid.NewGuid().ToString(), tenant);
        var sut = CreateHandler(db);
        var sb = Guid.NewGuid();
        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("99213", 120m)), CancellationToken.None);

        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("36415", 15m)), CancellationToken.None);

        var claim = await db.Claims.Include(c => c.Lines).SingleAsync();
        claim.Lines.Count.ShouldBe(1);
        claim.TotalCharge.ShouldBe(15m);
    }

    [Fact]
    public async Task Resave_Past_Draft_Is_Skipped_Freeze()
    {
        var tenant = new TestTenantAccessor();
        tenant.SetTenant(TestTenant);
        using var db = CreateContext(Guid.NewGuid().ToString(), tenant);
        var sut = CreateHandler(db);
        var sb = Guid.NewGuid();
        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("99213", 120m)), CancellationToken.None);
        var claim = await db.Claims.SingleAsync();
        claim.MarkReady();
        await db.SaveChangesAsync();

        await sut.HandleAsync(Event(sb, Guid.NewGuid(), Proc("36415", 15m)), CancellationToken.None);

        var after = await db.Claims.Include(c => c.Lines).SingleAsync();
        after.Status.ShouldBe(ClaimStatus.Ready);
        after.TotalCharge.ShouldBe(120m); // unchanged
        after.Lines.Single().Code.ShouldBe("99213");
    }

    [Fact]
    public async Task Redelivery_Does_Not_Create_Duplicate()
    {
        var tenant = new TestTenantAccessor();
        tenant.SetTenant(TestTenant);
        using var db = CreateContext(Guid.NewGuid().ToString(), tenant);
        var sut = CreateHandler(db);
        var sb = Guid.NewGuid();
        var evt = Event(sb, Guid.NewGuid(), Proc("99213", 120m));

        await sut.HandleAsync(evt, CancellationToken.None);
        await sut.HandleAsync(evt, CancellationToken.None);

        (await db.Claims.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Writes_Are_Isolated_To_The_Event_Tenant()
    {
        // Faithfully mimics the real pipeline: InMemoryEventBus opens FinbuckleEventTenantScope.Begin(
        // @event.TenantId) BEFORE it constructs the handler's ClaimsDbContext, and MultiTenantDbContext
        // captures its TenantInfo at construction. So the tenant is installed FIRST, then the DbContext is
        // built under it — that ordering is what makes the write tenant-scoped (a handler cannot retro-fit
        // tenancy onto an already-constructed context; see the handler's remarks and task-5-report.md).
        const string tenantA = "tenant-a";
        const string tenantB = "tenant-b";
        var root = new InMemoryDatabaseRoot();
        var dbName = Guid.NewGuid().ToString();
        var sb = Guid.NewGuid();

        // 1) Ambient starts unset; the pipeline installs the event's tenant, THEN builds the DbContext.
        var pipelineTenant = new TestTenantAccessor();
        pipelineTenant.SetTenant(tenantA); // == FinbuckleEventTenantScope.Begin(@event.TenantId)
        using (var handlerDb = CreateContext(dbName, pipelineTenant, root))
        {
            var sut = CreateHandler(handlerDb);
            await sut.HandleAsync(EventForTenant(sb, tenantA, Guid.NewGuid(), Proc("99213", 120m)), CancellationToken.None);
        }

        // 2) Tenant A sees the claim — the write was stamped with the event's tenant.
        var tenantAAccessor = new TestTenantAccessor();
        tenantAAccessor.SetTenant(tenantA);
        using (var dbA = CreateContext(dbName, tenantAAccessor, root))
        {
            (await dbA.Claims.CountAsync(c => c.SuperBillId == sb)).ShouldBe(1);
        }

        // 3) A different tenant sees nothing — the Finbuckle row filter isolates it.
        var tenantBAccessor = new TestTenantAccessor();
        tenantBAccessor.SetTenant(tenantB);
        using (var dbB = CreateContext(dbName, tenantBAccessor, root))
        {
            (await dbB.Claims.CountAsync(c => c.SuperBillId == sb)).ShouldBe(0);
        }
    }

    [Fact]
    public async Task Throws_When_Event_Has_No_TenantId()
    {
        // A null TenantId means the pipeline installed no tenant scope; fail fast rather than write under an
        // ambiguous tenant (mirrors Billing's TenantSubscribedIntegrationEventHandler).
        var tenant = new TestTenantAccessor();
        using var db = CreateContext(Guid.NewGuid().ToString(), tenant);
        var sut = CreateHandler(db);

        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await sut.HandleAsync(EventForTenant(Guid.NewGuid(), tenantId: null, Guid.NewGuid(), Proc("99213", 120m)), CancellationToken.None));
    }
}
