using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class IncidentTypeTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var type = IncidentType.Create("Slip and Fall");

        type.Id.ShouldNotBe(Guid.Empty);
        type.Name.ShouldBe("Slip and Fall");
        type.IsActive.ShouldBeTrue();
        type.IsDeleted.ShouldBeFalse();
        type.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName()
    {
        IncidentType.Create("  Auto Accident  ").Name.ShouldBe("Auto Accident");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => IncidentType.Create(name));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var type = IncidentType.Create("Slip and Fall");

        type.Update("Workplace Injury", isActive: false);

        type.Name.ShouldBe("Workplace Injury");
        type.IsActive.ShouldBeFalse();
        type.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var type = IncidentType.Create("Slip and Fall");

        type.Delete("admin@tenant");

        type.IsDeleted.ShouldBeTrue();
        type.DeletedOnUtc.ShouldNotBeNull();
        type.DeletedBy.ShouldBe("admin@tenant");
    }
}
