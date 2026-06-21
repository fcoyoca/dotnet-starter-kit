using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class DepartmentTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var dept = Department.Create("Cardiology", displayOrder: 3);

        dept.Id.ShouldNotBe(Guid.Empty);
        dept.Name.ShouldBe("Cardiology");
        dept.DisplayOrder.ShouldBe(3);
        dept.IsActive.ShouldBeTrue();
        dept.IsDeleted.ShouldBeFalse();
        dept.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName_And_DefaultOrderZero()
    {
        var dept = Department.Create("  Radiology  ");

        dept.Name.ShouldBe("Radiology");
        dept.DisplayOrder.ShouldBe(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => Department.Create(name));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var dept = Department.Create("Cardiology");

        dept.Update("Cardiology & Vascular", 5, isActive: false);

        dept.Name.ShouldBe("Cardiology & Vascular");
        dept.DisplayOrder.ShouldBe(5);
        dept.IsActive.ShouldBeFalse();
        dept.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var dept = Department.Create("Cardiology");

        dept.Delete("admin@tenant");

        dept.IsDeleted.ShouldBeTrue();
        dept.DeletedOnUtc.ShouldNotBeNull();
        dept.DeletedBy.ShouldBe("admin@tenant");
    }
}
