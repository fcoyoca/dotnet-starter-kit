using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class InsuranceCompanyTests
{
    [Fact]
    public void Create_Should_SetFields_And_DefaultActive()
    {
        var typeId = Guid.CreateVersion7();
        var company = InsuranceCompany.Create(
            "Blue Shield", typeId, 3, "1 Market St", "Suite 100", "San Francisco", "CA", "94105", "555-2000");

        company.Id.ShouldNotBe(Guid.Empty);
        company.Name.ShouldBe("Blue Shield");
        company.InsuranceTypeId.ShouldBe(typeId);
        company.FormularyTiers.ShouldBe(3);
        company.Address1.ShouldBe("1 Market St");
        company.City.ShouldBe("San Francisco");
        company.IsActive.ShouldBeTrue();
        company.IsDeleted.ShouldBeFalse();
        company.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_NullifyBlankOptionals_And_ClampTiers()
    {
        var company = InsuranceCompany.Create(
            "  Aetna ", null, -5, "   ", "", " Hartford ", null, "  ", null);

        company.Name.ShouldBe("Aetna");
        company.InsuranceTypeId.ShouldBeNull();
        company.FormularyTiers.ShouldBe(0);
        company.Address1.ShouldBeNull();
        company.City.ShouldBe("Hartford");
        company.Zip.ShouldBeNull();
        company.Phone.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() =>
            InsuranceCompany.Create(name, null, 0, null, null, null, null, null, null));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var company = InsuranceCompany.Create("Blue Shield", null, 0, null, null, null, null, null, null);
        var newType = Guid.CreateVersion7();

        company.Update("Blue Cross", newType, 5, "9 Oak", null, "Chicago", "IL", "60601", "555-9", isActive: false);

        company.Name.ShouldBe("Blue Cross");
        company.InsuranceTypeId.ShouldBe(newType);
        company.FormularyTiers.ShouldBe(5);
        company.City.ShouldBe("Chicago");
        company.IsActive.ShouldBeFalse();
        company.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Create_Should_PreserveLegacyId_When_Provided()
    {
        var company = InsuranceCompany.Create("Cigna", null, 0, null, null, null, null, null, null, legacyId: 55);

        company.LegacyId.ShouldBe(55);
    }

    [Fact]
    public void Delete_Should_SoftDelete()
    {
        var company = InsuranceCompany.Create("Blue Shield", null, 0, null, null, null, null, null, null);

        company.Delete("admin@tenant");

        company.IsDeleted.ShouldBeTrue();
        company.DeletedOnUtc.ShouldNotBeNull();
        company.DeletedBy.ShouldBe("admin@tenant");
    }
}
