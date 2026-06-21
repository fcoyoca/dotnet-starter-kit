using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class ProcedureCategoryTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var category = ProcedureCategory.Create("Radiology", "Imaging procedures", isImaging: true);

        category.Id.ShouldNotBe(Guid.Empty);
        category.Name.ShouldBe("Radiology");
        category.Description.ShouldBe("Imaging procedures");
        category.IsImaging.ShouldBeTrue();
        category.IsActive.ShouldBeTrue();
        category.IsDeleted.ShouldBeFalse();
        category.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName_And_NullBlankDescription()
    {
        var category = ProcedureCategory.Create("  Office Visits  ", "   ", isImaging: false);

        category.Name.ShouldBe("Office Visits");
        category.Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => ProcedureCategory.Create(name, null, false));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var category = ProcedureCategory.Create("Radiology", null, isImaging: true);

        category.Update("Imaging", "Updated", isImaging: false, isActive: false);

        category.Name.ShouldBe("Imaging");
        category.Description.ShouldBe("Updated");
        category.IsImaging.ShouldBeFalse();
        category.IsActive.ShouldBeFalse();
        category.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var category = ProcedureCategory.Create("Radiology", null, false);

        category.Delete("admin@tenant");

        category.IsDeleted.ShouldBeTrue();
        category.DeletedOnUtc.ShouldNotBeNull();
        category.DeletedBy.ShouldBe("admin@tenant");
    }
}
