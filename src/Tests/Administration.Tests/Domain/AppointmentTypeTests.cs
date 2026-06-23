using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class AppointmentTypeTests
{
    [Fact]
    public void Create_Should_SetFields_TrimName_And_DefaultActive()
    {
        var type = AppointmentType.Create("  Wellness  ", "  #ff99ff  ", 15, displayOrder: 2, legacyId: 12);

        type.Id.ShouldNotBe(Guid.Empty);
        type.Name.ShouldBe("Wellness");
        type.Color.ShouldBe("#ff99ff");
        type.DefaultDurationMinutes.ShouldBe(15);
        type.DisplayOrder.ShouldBe(2);
        type.LegacyId.ShouldBe(12);
        type.IsActive.ShouldBeTrue();
        type.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_NullBlankColor()
    {
        var type = AppointmentType.Create("X-ray", "   ", 10);
        type.Color.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameBlank(string name)
    {
        Should.Throw<ArgumentException>(() => AppointmentType.Create(name, "#000000", 15));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var type = AppointmentType.Create("Rehab", "#66cccc", 10);

        type.Update("Rehab Extended", "#3366cc", 30, displayOrder: 5, isActive: false);

        type.Name.ShouldBe("Rehab Extended");
        type.Color.ShouldBe("#3366cc");
        type.DefaultDurationMinutes.ShouldBe(30);
        type.DisplayOrder.ShouldBe(5);
        type.IsActive.ShouldBeFalse();
        type.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var type = AppointmentType.Create("Pool", "#3366aa", 30);

        type.Delete("admin@tenant");

        type.IsDeleted.ShouldBeTrue();
        type.DeletedOnUtc.ShouldNotBeNull();
        type.DeletedBy.ShouldBe("admin@tenant");
    }
}
