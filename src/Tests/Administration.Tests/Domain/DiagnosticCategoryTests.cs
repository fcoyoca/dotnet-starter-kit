using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class DiagnosticCategoryTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var category = DiagnosticCategory.Create("Cardiology");

        category.Id.ShouldNotBe(Guid.Empty);
        category.Name.ShouldBe("Cardiology");
        category.IsActive.ShouldBeTrue();
        category.IsDeleted.ShouldBeFalse();
        category.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName()
    {
        DiagnosticCategory.Create("  Neurology  ").Name.ShouldBe("Neurology");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => DiagnosticCategory.Create(name));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var category = DiagnosticCategory.Create("Cardiology");

        category.Update("Cardiovascular", isActive: false);

        category.Name.ShouldBe("Cardiovascular");
        category.IsActive.ShouldBeFalse();
        category.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var category = DiagnosticCategory.Create("Cardiology");

        category.Delete("admin@tenant");

        category.IsDeleted.ShouldBeTrue();
        category.DeletedOnUtc.ShouldNotBeNull();
        category.DeletedBy.ShouldBe("admin@tenant");
    }
}
