using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.SuperBills;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.SuperBills.GetReportProcedures;
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
}
