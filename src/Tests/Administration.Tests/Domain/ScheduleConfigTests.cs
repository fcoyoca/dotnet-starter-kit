using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class ScheduleConfigTests
{
    [Fact]
    public void Create_Should_SetFields_And_StampCreatedAt()
    {
        var clinicId = Guid.CreateVersion7();
        var config = ScheduleConfig.Create(clinicId, new TimeOnly(6, 30), new TimeOnly(21, 0), 15, legacyId: 1);

        config.Id.ShouldNotBe(Guid.Empty);
        config.ClinicId.ShouldBe(clinicId);
        config.StartTime.ShouldBe(new TimeOnly(6, 30));
        config.EndTime.ShouldBe(new TimeOnly(21, 0));
        config.IntervalMinutes.ShouldBe(15);
        config.LegacyId.ShouldBe(1);
        config.CreatedAtUtc.ShouldNotBe(default);
    }

    [Fact]
    public void Create_Should_Throw_When_ClinicIdEmpty()
    {
        Should.Throw<ArgumentException>(() =>
            ScheduleConfig.Create(Guid.Empty, new TimeOnly(8, 0), new TimeOnly(17, 0), 15));
    }

    [Fact]
    public void Create_Should_Throw_When_EndNotAfterStart()
    {
        var clinicId = Guid.CreateVersion7();
        Should.Throw<ArgumentException>(() =>
            ScheduleConfig.Create(clinicId, new TimeOnly(17, 0), new TimeOnly(17, 0), 15));
    }

    [Fact]
    public void Create_Should_Throw_When_IntervalNotPositive()
    {
        var clinicId = Guid.CreateVersion7();
        Should.Throw<ArgumentException>(() =>
            ScheduleConfig.Create(clinicId, new TimeOnly(8, 0), new TimeOnly(17, 0), 0));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var config = ScheduleConfig.Create(Guid.CreateVersion7(), new TimeOnly(8, 0), new TimeOnly(17, 0), 15);

        config.Update(new TimeOnly(7, 0), new TimeOnly(19, 0), 30);

        config.StartTime.ShouldBe(new TimeOnly(7, 0));
        config.EndTime.ShouldBe(new TimeOnly(19, 0));
        config.IntervalMinutes.ShouldBe(30);
        config.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Update_Should_Throw_When_EndNotAfterStart()
    {
        var config = ScheduleConfig.Create(Guid.CreateVersion7(), new TimeOnly(8, 0), new TimeOnly(17, 0), 15);

        Should.Throw<ArgumentException>(() => config.Update(new TimeOnly(18, 0), new TimeOnly(17, 0), 15));
    }
}
