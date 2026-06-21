using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class ProcedureCodeTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var categoryId = Guid.CreateVersion7();
        var code = ProcedureCode.Create("99213", "Office visit", "Established patient", categoryId, 1, "Note macro");

        code.Id.ShouldNotBe(Guid.Empty);
        code.Code.ShouldBe("99213");
        code.Name.ShouldBe("Office visit");
        code.Description.ShouldBe("Established patient");
        code.ProcedureCategoryId.ShouldBe(categoryId);
        code.CodeSourceId.ShouldBe(1);
        code.MacroText.ShouldBe("Note macro");
        code.IsActive.ShouldBeTrue();
        code.IsDeleted.ShouldBeFalse();
        code.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimCode_And_NullBlankOptionalFields()
    {
        var categoryId = Guid.CreateVersion7();
        var code = ProcedureCode.Create("  99214  ", "   ", "", categoryId, null, "");

        code.Code.ShouldBe("99214");
        code.Name.ShouldBeNull();
        code.Description.ShouldBeNull();
        code.ProcedureCategoryId.ShouldBe(categoryId);
        code.CodeSourceId.ShouldBeNull();
        code.MacroText.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_CodeIsBlank(string code)
    {
        Should.Throw<ArgumentException>(() => ProcedureCode.Create(code, null, null, Guid.CreateVersion7(), null, null));
    }

    [Fact]
    public void Create_Should_Throw_When_ProcedureCategoryIdEmpty()
    {
        Should.Throw<ArgumentException>(() => ProcedureCode.Create("99213", null, null, Guid.Empty, null, null));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var code = ProcedureCode.Create("99213", "Office visit", null, Guid.CreateVersion7(), 1, null);
        var categoryId = Guid.CreateVersion7();

        code.Update("99215", "Complex visit", "Long", categoryId, 2, "Macro", isActive: false);

        code.Code.ShouldBe("99215");
        code.Name.ShouldBe("Complex visit");
        code.Description.ShouldBe("Long");
        code.ProcedureCategoryId.ShouldBe(categoryId);
        code.CodeSourceId.ShouldBe(2);
        code.MacroText.ShouldBe("Macro");
        code.IsActive.ShouldBeFalse();
        code.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var code = ProcedureCode.Create("99213", null, null, Guid.CreateVersion7(), null, null);

        code.Delete("admin@tenant");

        code.IsDeleted.ShouldBeTrue();
        code.DeletedOnUtc.ShouldNotBeNull();
        code.DeletedBy.ShouldBe("admin@tenant");
    }
}
