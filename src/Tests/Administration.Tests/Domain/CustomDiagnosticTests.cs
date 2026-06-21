using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class CustomDiagnosticTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var dx = CustomDiagnostic.Create("M54.5", "Low back pain", "Chronic low back pain", isChiropractic: true);

        dx.Id.ShouldNotBe(Guid.Empty);
        dx.Code.ShouldBe("M54.5");
        dx.Description.ShouldBe("Low back pain");
        dx.LongDescription.ShouldBe("Chronic low back pain");
        dx.IsChiropractic.ShouldBeTrue();
        dx.IsActive.ShouldBeTrue();
        dx.IsDeleted.ShouldBeFalse();
        dx.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimCode_And_NullBlankOptionalFields()
    {
        var dx = CustomDiagnostic.Create("  M99  ", "   ", "", isChiropractic: false);

        dx.Code.ShouldBe("M99");
        dx.Description.ShouldBeNull();
        dx.LongDescription.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_CodeIsBlank(string code)
    {
        Should.Throw<ArgumentException>(() => CustomDiagnostic.Create(code, null, null, false));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var dx = CustomDiagnostic.Create("M54.5", "Low back pain", null, isChiropractic: true);

        dx.Update("M54.6", "Updated", "Long", isChiropractic: false, isActive: false);

        dx.Code.ShouldBe("M54.6");
        dx.Description.ShouldBe("Updated");
        dx.LongDescription.ShouldBe("Long");
        dx.IsChiropractic.ShouldBeFalse();
        dx.IsActive.ShouldBeFalse();
        dx.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var dx = CustomDiagnostic.Create("M54.5", null, null, false);

        dx.Delete("admin@tenant");

        dx.IsDeleted.ShouldBeTrue();
        dx.DeletedOnUtc.ShouldNotBeNull();
        dx.DeletedBy.ShouldBe("admin@tenant");
    }
}
