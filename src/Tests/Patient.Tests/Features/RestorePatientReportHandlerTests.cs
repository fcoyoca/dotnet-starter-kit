using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using FSH.Modules.Patient.Features.v1.PatientReports.RestorePatientReport;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class RestorePatientReportHandlerTests
{
    private static async Task<PatientReport> SeedDeletedReport(PatientDbContext db)
    {
        PatientReport report = PatientReport.Create(
            incidentId: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
            reportTypeId: 7,
            reportDate: DateTime.UtcNow.Date,
            providerId: null,
            clinicId: null,
            isNoShow: false);
        report.Delete("tester");
        db.PatientReports.Add(report);
        await db.SaveChangesAsync();
        return report;
    }

    [Fact]
    public async Task Restore_Should_Clear_Deletion_State()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedDeletedReport(db);
        var sut = new RestorePatientReportCommandHandler(db);

        await sut.Handle(new RestorePatientReportCommand(report.Id), CancellationToken.None);

        PatientReport saved = await db.PatientReports
            .IgnoreQueryFilters()
            .SingleAsync(x => x.Id == report.Id);
        saved.IsDeleted.ShouldBeFalse();
        saved.DeletedOnUtc.ShouldBeNull();
        saved.DeletedBy.ShouldBeNull();
    }

    [Fact]
    public async Task Restore_Should_Throw_NotFound_For_Unknown_Report()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        var sut = new RestorePatientReportCommandHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new RestorePatientReportCommand(Guid.NewGuid()), CancellationToken.None).AsTask());
    }

    [Fact]
    public async Task Restore_Should_Throw_NotFound_When_Report_Is_Not_Deleted()
    {
        using PatientDbContext db = SuperBillHandlerTests.CreateContext(Guid.NewGuid().ToString());
        PatientReport report = await SeedDeletedReport(db);
        report.Restore();
        await db.SaveChangesAsync();
        var sut = new RestorePatientReportCommandHandler(db);

        await Should.ThrowAsync<NotFoundException>(
            () => sut.Handle(new RestorePatientReportCommand(report.Id), CancellationToken.None).AsTask());
    }
}
