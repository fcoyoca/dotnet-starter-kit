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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Claims.Tests.Features;

public sealed class SuperBillSavedIntegrationEventHandlerTests
{
    private static ClaimsDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(new MultiTenantContext<AppTenantInfo>(
            new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));
        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql", ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });
        return new ClaimsDbContext(accessor, options, settings, Substitute.For<IHostEnvironment>());
    }

    private static SuperBillSavedIntegrationEvent Event(
        Guid superBillId, Guid? insuranceTypeId, params ReportProcedureItem[] procedures) =>
        new(
            Id: Guid.NewGuid(), OccurredOnUtc: DateTime.UtcNow, TenantId: "test",
            CorrelationId: Guid.NewGuid().ToString(), Source: "Patient",
            SuperBillId: superBillId, ReportId: Guid.NewGuid(), PatientId: Guid.NewGuid(),
            IsBilled: false, Procedures: procedures, InsuranceTypeId: insuranceTypeId);

    private static ReportProcedureItem Proc(string code, decimal charge) =>
        new(ProcedureCodeId: Guid.NewGuid(), Code: code, Description: code, Charge: charge, DiagnosticIds: []);

    [Fact]
    public async Task Creates_Draft_Claim_Snapshotting_Lines_And_Total()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
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
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
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
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
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
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new SuperBillSavedIntegrationEventHandler(db, NullLogger<SuperBillSavedIntegrationEventHandler>.Instance);
        var sb = Guid.NewGuid();
        var evt = Event(sb, Guid.NewGuid(), Proc("99213", 120m));

        await sut.HandleAsync(evt, CancellationToken.None);
        await sut.HandleAsync(evt, CancellationToken.None);

        (await db.Claims.CountAsync()).ShouldBe(1);
    }
}
