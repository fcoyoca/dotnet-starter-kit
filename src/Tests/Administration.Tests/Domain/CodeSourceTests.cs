using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class CodeSourceTests
{
    [Fact]
    public void Create_Should_TrimName_And_DefaultActive()
    {
        var source = CodeSource.Create("  CPT  ");

        source.Name.ShouldBe("CPT");
        source.IsActive.ShouldBeTrue();
        source.IsDeleted.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => CodeSource.Create(name));
    }

    [Fact]
    public void Update_Should_MutateFields()
    {
        var source = CodeSource.Create("CPT");

        source.Update("HCPCS", isActive: false);

        source.Name.ShouldBe("HCPCS");
        source.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var source = CodeSource.Create("CPT");

        source.Delete("tester");

        source.IsDeleted.ShouldBeTrue();
        source.DeletedOnUtc.ShouldNotBeNull();
        source.DeletedBy.ShouldBe("tester");
    }
}
