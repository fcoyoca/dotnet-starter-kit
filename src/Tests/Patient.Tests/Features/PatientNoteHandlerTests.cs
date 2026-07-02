using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Patient.Contracts.v1.PatientNotes;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Features.v1.PatientNotes.CreatePatientNote;
using FSH.Modules.Patient.Features.v1.PatientNotes.DeletePatientNote;
using FSH.Modules.Patient.Features.v1.PatientNotes.SearchPatientNotes;
using FSH.Modules.Patient.Features.v1.PatientNotes.UpdatePatientNote;
using FSH.Modules.Patient.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Patient.Tests.Features;

public sealed class PatientNoteHandlerTests
{
    private static PatientDbContext CreateContext(string dbName)
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

    private static ICurrentUser User()
    {
        var user = Substitute.For<ICurrentUser>();
        user.GetUserId().Returns(Guid.NewGuid());
        user.Name.Returns("Test User");
        return user;
    }

    [Fact]
    public async Task Create_Then_Update_Then_SoftDelete_Roundtrip()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        var create = new CreatePatientNoteCommandHandler(db, User());
        var update = new UpdatePatientNoteCommandHandler(db, User());
        var delete = new DeletePatientNoteCommandHandler(db, User());

        Guid id = await create.Handle(
            new CreatePatientNoteCommand(patientId, "Fall risk", "Uses a cane", true), CancellationToken.None);

        var saved = await db.PatientNotes.FindAsync(id);
        saved!.Name.ShouldBe("Fall risk");
        saved.IsMedicalAlert.ShouldBeTrue();

        await update.Handle(new UpdatePatientNoteCommand(id, "Fall risk", "Uses a walker", false), CancellationToken.None);
        (await db.PatientNotes.FindAsync(id))!.Description.ShouldBe("Uses a walker");

        await delete.Handle(new DeletePatientNoteCommand(id), CancellationToken.None);
        (await db.PatientNotes.FindAsync(id))!.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task Search_Should_Filter_Deleted_And_MedicalAlerts()
    {
        using var db = CreateContext(Guid.NewGuid().ToString());
        Guid patientId = Guid.NewGuid();
        var create = new CreatePatientNoteCommandHandler(db, User());
        var delete = new DeletePatientNoteCommandHandler(db, User());
        Guid keep = await create.Handle(new CreatePatientNoteCommand(patientId, "Alert note", null, true), CancellationToken.None);
        Guid gone = await create.Handle(new CreatePatientNoteCommand(patientId, "Deleted note", null, false), CancellationToken.None);
        await delete.Handle(new DeletePatientNoteCommand(gone), CancellationToken.None);
        var sut = new SearchPatientNotesQueryHandler(db);

        PagedResponse<FSH.Modules.Patient.Contracts.Dtos.PatientNoteDto> visible =
            await sut.Handle(new SearchPatientNotesQuery(patientId), CancellationToken.None);
        PagedResponse<FSH.Modules.Patient.Contracts.Dtos.PatientNoteDto> alerts =
            await sut.Handle(new SearchPatientNotesQuery(patientId, MedicalAlertsOnly: true), CancellationToken.None);

        visible.Items.Count.ShouldBe(1);
        visible.Items.Single().Id.ShouldBe(keep);
        alerts.Items.Count.ShouldBe(1);
        alerts.Items.Single().IsMedicalAlert.ShouldBeTrue();
    }
}
