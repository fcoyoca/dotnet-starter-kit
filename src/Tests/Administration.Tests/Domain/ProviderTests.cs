using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class ProviderTests
{
    [Fact]
    public void Create_Should_SetCoreFields_And_DefaultActive()
    {
        var clinicId = Guid.CreateVersion7();
        var provider = Provider.Create(
            "Gregory", "House", "Dr.", "MD", "Diagnostics",
            "1234567890", "KAREO-1", clinicId, "user-123");

        provider.Id.ShouldNotBe(Guid.Empty);
        provider.FirstName.ShouldBe("Gregory");
        provider.LastName.ShouldBe("House");
        provider.Prefix.ShouldBe("Dr.");
        provider.Suffix.ShouldBe("MD");
        provider.Specialty.ShouldBe("Diagnostics");
        provider.Npi.ShouldBe("1234567890");
        provider.KareoExternalId.ShouldBe("KAREO-1");
        provider.PrimaryClinicId.ShouldBe(clinicId);
        provider.UserId.ShouldBe("user-123");
        provider.IsActive.ShouldBeTrue();
        provider.IsDeleted.ShouldBeFalse();
        provider.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_NullifyBlankOptionals_And_Trim()
    {
        var provider = Provider.Create(
            "  Lisa ", " Cuddy ", "  ", "", "   ",
            null, "  ", null, "   ");

        provider.FirstName.ShouldBe("Lisa");
        provider.LastName.ShouldBe("Cuddy");
        provider.Prefix.ShouldBeNull();
        provider.Suffix.ShouldBeNull();
        provider.Specialty.ShouldBeNull();
        provider.Npi.ShouldBeNull();
        provider.KareoExternalId.ShouldBeNull();
        provider.PrimaryClinicId.ShouldBeNull();
        provider.UserId.ShouldBeNull();
    }

    [Theory]
    [InlineData("", "House")]
    [InlineData("   ", "House")]
    [InlineData("Gregory", "")]
    [InlineData("Gregory", "   ")]
    public void Create_Should_Throw_When_NameIsBlank(string first, string last)
    {
        Should.Throw<ArgumentException>(() =>
            Provider.Create(first, last, null, null, null, null, null, null, null));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);
        var newClinic = Guid.CreateVersion7();

        provider.Update("James", "Wilson", "Dr.", "MD", "Oncology",
            "9876543210", "KAREO-9", newClinic, "user-9", isActive: false);

        provider.FirstName.ShouldBe("James");
        provider.LastName.ShouldBe("Wilson");
        provider.Specialty.ShouldBe("Oncology");
        provider.Npi.ShouldBe("9876543210");
        provider.PrimaryClinicId.ShouldBe(newClinic);
        provider.UserId.ShouldBe("user-9");
        provider.IsActive.ShouldBeFalse();
        provider.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Create_Should_PreserveLegacyUserId_When_Provided()
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null, legacyUserId: 77);

        provider.LegacyUserId.ShouldBe(77);
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);

        provider.Delete("admin@tenant");

        provider.IsDeleted.ShouldBeTrue();
        provider.DeletedOnUtc.ShouldNotBeNull();
        provider.DeletedBy.ShouldBe("admin@tenant");
    }

    [Fact]
    public void SetSignature_Should_StorePath_And_StampUpdatedAt()
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);

        provider.SetSignature("administration/providers/abc/signature.png");

        provider.SignatureImagePath.ShouldBe("administration/providers/abc/signature.png");
        provider.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SetSignature_Should_Throw_When_PathBlank(string path)
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);

        Should.Throw<ArgumentException>(() => provider.SetSignature(path));
    }

    [Fact]
    public void ClearSignature_Should_NullPath_And_StampUpdatedAt()
    {
        var provider = Provider.Create("Gregory", "House", null, null, null, null, null, null, null);
        provider.SetSignature("administration/providers/abc/signature.png");

        provider.ClearSignature();

        provider.SignatureImagePath.ShouldBeNull();
        provider.UpdatedAtUtc.ShouldNotBeNull();
    }
}
