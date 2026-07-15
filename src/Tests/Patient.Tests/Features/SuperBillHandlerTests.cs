using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.Events;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;
using FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class SuperBillHandlerTests
{
    internal static PatientDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<PatientDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));

        var settings = Options.Create(new DatabaseOptions
        {
            Provider = "postgresql",
            ConnectionString = string.Empty,
            MigrationsAssembly = "FSH.Starter.Migrations.PostgreSQL",
        });

        return new PatientDbContext(
            accessor, options, settings, Substitute.For<IHostEnvironment>(), Substitute.For<IPhiEncryptor>());
    }

    internal static async Task<PatientReport> SeedReport(PatientDbContext db)
    {
        PatientReport report = PatientReport.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1, DateTime.UtcNow.Date, null, null, false);
        db.PatientReports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    [Fact]
    public async Task Get_Should_Return_Empty_Shell_When_No_SuperBill_Exists()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        var sut = new GetReportProceduresQueryHandler(db);

        SuperBillDto dto = await sut.Handle(new GetReportProceduresQuery(report.Id), CancellationToken.None);

        dto.Id.ShouldBeNull();
        dto.ReportId.ShouldBe(report.Id);
        dto.IsBilled.ShouldBeFalse();
        dto.Procedures.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_Should_Throw_NotFound_For_Unknown_Report()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = new GetReportProceduresQueryHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new GetReportProceduresQuery(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Get_Should_Return_Procedures_Ordered_By_DisplayOrder()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        SuperBill bill = SuperBill.Create(report.Id, report.PatientId);
        bill.ReplaceProcedures(
        [
            new ReportProcedureItem(Guid.NewGuid(), "98940", "One to two spinal regions", 20m, [dx]),
            new ReportProcedureItem(Guid.NewGuid(), "97110", "Therapeutic exercises", 35.5m, [dx]),
        ]);
        db.SuperBills.Add(bill);
        await db.SaveChangesAsync();
        var sut = new GetReportProceduresQueryHandler(db);

        SuperBillDto dto = await sut.Handle(new GetReportProceduresQuery(report.Id), CancellationToken.None);

        dto.Id.ShouldBe(bill.Id);
        dto.Procedures.Count.ShouldBe(2);
        dto.Procedures[0].Code.ShouldBe("98940");
        dto.Procedures[1].Code.ShouldBe("97110");
        dto.Procedures[0].DiagnosticIds.Single().ShouldBe(dx);
    }

    private static SetReportProceduresCommandHandler CreateSetHandler(PatientDbContext db, IEventBus? bus = null)
    {
        var accessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        accessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(
                new AppTenantInfo("test", "test", string.Empty, "test@test.com", "test")));
        return new SetReportProceduresCommandHandler(
            db, bus ?? Substitute.For<IEventBus>(), accessor, TimeProvider.System);
    }

    [Fact]
    public async Task Set_Should_Create_SuperBill_On_First_Save_And_Publish_Event()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        IEventBus bus = Substitute.For<IEventBus>();
        Guid dx = Guid.NewGuid();
        var sut = CreateSetHandler(db, bus);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
        [
            new ReportProcedureItem(Guid.NewGuid(), "98940", "One to two spinal regions", 20m, [dx]),
        ]), CancellationToken.None);

        SuperBill saved = await db.SuperBills.Include(x => x.Procedures).ThenInclude(p => p.Diagnostics)
            .SingleAsync(x => x.ReportId == report.Id);
        saved.PatientId.ShouldBe(report.PatientId);
        saved.Procedures.Single().Code.ShouldBe("98940");
        saved.Procedures.Single().Diagnostics.Single().DiagnosticId.ShouldBe(dx);
        await bus.Received(1).PublishAsync(
            Arg.Is<SuperBillSavedIntegrationEvent>(e =>
                e.ReportId == report.Id && e.Source == "Patient" && e.Procedures.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_Should_Replace_Existing_Set()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        var sut = CreateSetHandler(db);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
        [
            new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx]),
            new ReportProcedureItem(Guid.NewGuid(), "97110", null, 35m, [dx]),
        ]), CancellationToken.None);
        await sut.Handle(new SetReportProceduresCommand(report.Id,
        [
            new ReportProcedureItem(Guid.NewGuid(), "97140", null, 40m, [dx]),
        ]), CancellationToken.None);

        SuperBill saved = await db.SuperBills.Include(x => x.Procedures)
            .SingleAsync(x => x.ReportId == report.Id);
        saved.Procedures.Single().Code.ShouldBe("97140");
        (await db.SuperBills.CountAsync()).ShouldBe(1); // still one super bill per report
    }

    [Fact]
    public async Task Set_Should_Allow_Empty_List_To_Clear()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        var sut = CreateSetHandler(db);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
            [new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx])]), CancellationToken.None);
        await sut.Handle(new SetReportProceduresCommand(report.Id, []), CancellationToken.None);

        SuperBill saved = await db.SuperBills.Include(x => x.Procedures)
            .SingleAsync(x => x.ReportId == report.Id);
        saved.Procedures.ShouldBeEmpty();
    }

    [Fact]
    public async Task Set_Should_Throw_NotFound_For_Unknown_Report()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        var sut = CreateSetHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new SetReportProceduresCommand(Guid.NewGuid(), []), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Set_Should_Snapshot_InsuranceType_And_RoundTrip_Through_Get_And_Event()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        IEventBus bus = Substitute.For<IEventBus>();
        Guid dx = Guid.NewGuid();
        Guid insuranceType = Guid.NewGuid();
        var setSut = CreateSetHandler(db, bus);

        await setSut.Handle(new SetReportProceduresCommand(report.Id,
            [new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx])],
            InsuranceTypeId: insuranceType), CancellationToken.None);

        SuperBill saved = await db.SuperBills.SingleAsync(x => x.ReportId == report.Id);
        saved.InsuranceTypeId.ShouldBe(insuranceType);

        SuperBillDto dto = await new GetReportProceduresQueryHandler(db)
            .Handle(new GetReportProceduresQuery(report.Id), CancellationToken.None);
        dto.InsuranceTypeId.ShouldBe(insuranceType);

        await bus.Received(1).PublishAsync(
            Arg.Is<SuperBillSavedIntegrationEvent>(e => e.InsuranceTypeId == insuranceType),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_Should_Update_InsuranceType_On_Resave()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        Guid firstType = Guid.NewGuid();
        Guid secondType = Guid.NewGuid();
        var sut = CreateSetHandler(db);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
            [new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx])],
            InsuranceTypeId: firstType), CancellationToken.None);
        await sut.Handle(new SetReportProceduresCommand(report.Id,
            [new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx])],
            InsuranceTypeId: secondType), CancellationToken.None);

        SuperBill saved = await db.SuperBills.SingleAsync(x => x.ReportId == report.Id);
        saved.InsuranceTypeId.ShouldBe(secondType);
    }

    [Fact]
    public async Task Set_Should_Leave_InsuranceType_Null_When_Not_Provided()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedReport(db);
        Guid dx = Guid.NewGuid();
        var sut = CreateSetHandler(db);

        await sut.Handle(new SetReportProceduresCommand(report.Id,
            [new ReportProcedureItem(Guid.NewGuid(), "98940", null, 20m, [dx])]), CancellationToken.None);

        SuperBill saved = await db.SuperBills.SingleAsync(x => x.ReportId == report.Id);
        saved.InsuranceTypeId.ShouldBeNull();
    }
}
