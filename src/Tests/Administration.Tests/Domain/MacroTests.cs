using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class MacroTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var owner = Guid.NewGuid();
        var macro = Macro.Create("Normal Exam", "Patient is well-appearing and in no acute distress.", reportFieldId: 13, useableByUserId: owner);

        macro.Id.ShouldNotBe(Guid.Empty);
        macro.Name.ShouldBe("Normal Exam");
        macro.Text.ShouldBe("Patient is well-appearing and in no acute distress.");
        macro.ReportFieldId.ShouldBe(13);
        macro.UseableByUserId.ShouldBe(owner);
        macro.IsActive.ShouldBeTrue();
        macro.IsDeleted.ShouldBeFalse();
        macro.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName_And_NullBlankText()
    {
        var macro = Macro.Create("  Follow Up  ", "   ", reportFieldId: null, useableByUserId: null);
        macro.Name.ShouldBe("Follow Up");
        macro.Text.ShouldBeNull();
        macro.ReportFieldId.ShouldBeNull();
        macro.UseableByUserId.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => Macro.Create(name, null, null, null));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var macro = Macro.Create("Normal Exam", "old body", reportFieldId: 1, useableByUserId: null);
        var owner = Guid.NewGuid();

        macro.Update("Detailed Exam", "new body", reportFieldId: 24, useableByUserId: owner, isActive: false);

        macro.Name.ShouldBe("Detailed Exam");
        macro.Text.ShouldBe("new body");
        macro.ReportFieldId.ShouldBe(24);
        macro.UseableByUserId.ShouldBe(owner);
        macro.IsActive.ShouldBeFalse();
        macro.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var macro = Macro.Create("Normal Exam", null, null, null);

        macro.Delete("admin@tenant");

        macro.IsDeleted.ShouldBeTrue();
        macro.DeletedOnUtc.ShouldNotBeNull();
        macro.DeletedBy.ShouldBe("admin@tenant");
    }
}
