using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class InsuranceTypeTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var type = InsuranceType.Create("PPO");

        type.Id.ShouldNotBe(Guid.Empty);
        type.Name.ShouldBe("PPO");
        type.IsActive.ShouldBeTrue();
        type.IsDeleted.ShouldBeFalse();
        type.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName()
    {
        InsuranceType.Create("  HMO  ").Name.ShouldBe("HMO");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => InsuranceType.Create(name));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var type = InsuranceType.Create("PPO");

        type.Update("Preferred Provider Org", isActive: false);

        type.Name.ShouldBe("Preferred Provider Org");
        type.IsActive.ShouldBeFalse();
        type.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var type = InsuranceType.Create("PPO");

        type.Delete("admin@tenant");

        type.IsDeleted.ShouldBeTrue();
        type.DeletedOnUtc.ShouldNotBeNull();
        type.DeletedBy.ShouldBe("admin@tenant");
    }
}
