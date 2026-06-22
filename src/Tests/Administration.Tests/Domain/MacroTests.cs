using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class MacroTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var macro = Macro.Create("Normal Exam", "Patient is well-appearing and in no acute distress.");

        macro.Id.ShouldNotBe(Guid.Empty);
        macro.Name.ShouldBe("Normal Exam");
        macro.Text.ShouldBe("Patient is well-appearing and in no acute distress.");
        macro.IsActive.ShouldBeTrue();
        macro.IsDeleted.ShouldBeFalse();
        macro.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName_And_NullBlankText()
    {
        var macro = Macro.Create("  Follow Up  ", "   ");
        macro.Name.ShouldBe("Follow Up");
        macro.Text.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => Macro.Create(name, null));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var macro = Macro.Create("Normal Exam", "old body");

        macro.Update("Detailed Exam", "new body", isActive: false);

        macro.Name.ShouldBe("Detailed Exam");
        macro.Text.ShouldBe("new body");
        macro.IsActive.ShouldBeFalse();
        macro.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var macro = Macro.Create("Normal Exam", null);

        macro.Delete("admin@tenant");

        macro.IsDeleted.ShouldBeTrue();
        macro.DeletedOnUtc.ShouldNotBeNull();
        macro.DeletedBy.ShouldBe("admin@tenant");
    }
}
