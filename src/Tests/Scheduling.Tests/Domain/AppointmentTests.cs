using FSH.Modules.Scheduling.Domain;

namespace Scheduling.Tests.Domain;

public sealed class AppointmentTests
{
    private static Appointment New() => Appointment.Create(
        Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
        new DateTime(2026, 6, 24, 14, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 6, 24, 14, 30, 0, DateTimeKind.Utc), "  hi  ");

    [Fact]
    public void Create_Should_Set_Defaults_And_TrimNotes()
    {
        var a = New();
        a.Id.ShouldNotBe(Guid.Empty);
        a.Status.ShouldBe(AppointmentStatus.Scheduled);
        a.Cancelled.ShouldBeFalse();
        a.NoShow.ShouldBeFalse();
        a.Notes.ShouldBe("hi");
        a.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_Throw_When_EndNotAfterStart()
    {
        var t = new DateTime(2026, 6, 24, 14, 0, 0, DateTimeKind.Utc);
        Should.Throw<ArgumentException>(() =>
            Appointment.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), null, null, t, t, null));
    }

    [Fact]
    public void Create_Should_Throw_When_ClinicOrProviderEmpty()
    {
        var s = new DateTime(2026, 6, 24, 14, 0, 0, DateTimeKind.Utc);
        var e = s.AddMinutes(30);
        Should.Throw<ArgumentException>(() =>
            Appointment.Create(Guid.Empty, Guid.CreateVersion7(), null, null, s, e, null));
        Should.Throw<ArgumentException>(() =>
            Appointment.Create(Guid.CreateVersion7(), Guid.Empty, null, null, s, e, null));
    }

    [Fact]
    public void Lifecycle_CheckIn_Then_CheckOut_Advances_Status()
    {
        var a = New();
        a.CheckIn();
        a.Status.ShouldBe(AppointmentStatus.CheckedIn);
        a.CheckOut();
        a.Status.ShouldBe(AppointmentStatus.CheckedOut);
    }

    [Fact]
    public void Cancel_And_NoShow_Set_Flags()
    {
        var a = New();
        a.Cancel();
        a.Cancelled.ShouldBeTrue();

        var b = New();
        b.MarkNoShow();
        b.NoShow.ShouldBeTrue();
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var a = New();
        var newClinic = Guid.CreateVersion7();
        var newStart = new DateTime(2026, 6, 25, 9, 0, 0, DateTimeKind.Utc);
        var newEnd = newStart.AddHours(1);

        a.Update(newClinic, a.ProviderId, null, null, newStart, newEnd, "rebooked");

        a.ClinicId.ShouldBe(newClinic);
        a.PatientId.ShouldBeNull();
        a.StartUtc.ShouldBe(newStart);
        a.EndUtc.ShouldBe(newEnd);
        a.Notes.ShouldBe("rebooked");
        a.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var a = New();
        a.Delete("admin@tenant");
        a.IsDeleted.ShouldBeTrue();
        a.DeletedOnUtc.ShouldNotBeNull();
        a.DeletedBy.ShouldBe("admin@tenant");
    }

    [Fact]
    public void Create_Reservation_Should_RequireTitle_And_NullPatientAndType()
    {
        var s = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var a = Appointment.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7(),
            s, s.AddMinutes(30), "lunch break", isReservation: true, reservationTitle: "  Lunch  ");

        a.IsReservation.ShouldBeTrue();
        a.ReservationTitle.ShouldBe("Lunch");
        a.PatientId.ShouldBeNull();
        a.AppointmentTypeId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Reservation_Should_Throw_When_TitleBlank(string title)
    {
        var s = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        Should.Throw<ArgumentException>(() => Appointment.Create(
            Guid.CreateVersion7(), Guid.CreateVersion7(), null, null,
            s, s.AddMinutes(30), null, isReservation: true, reservationTitle: title));
    }

    [Fact]
    public void Update_To_Reservation_Should_ClearPatientAndType()
    {
        var a = New(); // a patient appointment with patient + type set
        a.PatientId.ShouldNotBeNull();

        a.Update(a.ClinicId, a.ProviderId, a.PatientId, a.AppointmentTypeId,
            a.StartUtc, a.EndUtc, a.Notes, isReservation: true, reservationTitle: "Hold");

        a.IsReservation.ShouldBeTrue();
        a.ReservationTitle.ShouldBe("Hold");
        a.PatientId.ShouldBeNull();
        a.AppointmentTypeId.ShouldBeNull();
    }
}
