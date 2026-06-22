using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class PatientDocumentTypeTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var type = PatientDocumentType.Create("Lab Result");

        type.Id.ShouldNotBe(Guid.Empty);
        type.Name.ShouldBe("Lab Result");
        type.IsActive.ShouldBeTrue();
        type.IsDeleted.ShouldBeFalse();
        type.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimName()
    {
        PatientDocumentType.Create("  Referral Letter  ").Name.ShouldBe("Referral Letter");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() => PatientDocumentType.Create(name));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var type = PatientDocumentType.Create("Lab Result");

        type.Update("Insurance Card", isActive: false);

        type.Name.ShouldBe("Insurance Card");
        type.IsActive.ShouldBeFalse();
        type.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var type = PatientDocumentType.Create("Lab Result");

        type.Delete("admin@tenant");

        type.IsDeleted.ShouldBeTrue();
        type.DeletedOnUtc.ShouldNotBeNull();
        type.DeletedBy.ShouldBe("admin@tenant");
    }
}
