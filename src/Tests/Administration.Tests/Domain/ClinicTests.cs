using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class ClinicTests
{
    private static Clinic Valid() =>
        Clinic.Create("C-001", "Main Clinic", "123 Main St", null, "Springfield", "IL", "62704", "555-1000");

    [Fact]
    public void Create_Should_SetAllRequiredFields_When_Valid()
    {
        var clinic = Valid();

        clinic.Id.ShouldNotBe(Guid.Empty);
        clinic.Code.ShouldBe("C-001");
        clinic.Name.ShouldBe("Main Clinic");
        clinic.Address1.ShouldBe("123 Main St");
        clinic.City.ShouldBe("Springfield");
        clinic.State.ShouldBe("IL");
        clinic.Zip.ShouldBe("62704");
        clinic.Phone.ShouldBe("555-1000");
        clinic.IsActive.ShouldBeTrue();
        clinic.IsDeleted.ShouldBeFalse();
        clinic.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_TrimFields_And_NullifyBlankOptionals()
    {
        var clinic = Clinic.Create("  C-002 ", "  Annex ", " 1 Elm ", "   ", " Metropolis ", " NY ", " 10001 ", "   ");

        clinic.Code.ShouldBe("C-002");
        clinic.Name.ShouldBe("Annex");
        clinic.Address1.ShouldBe("1 Elm");
        clinic.Address2.ShouldBeNull();
        clinic.City.ShouldBe("Metropolis");
        clinic.Phone.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_CodeIsBlank(string code)
    {
        Should.Throw<ArgumentException>(() =>
            Clinic.Create(code, "Name", "Addr", null, "City", "ST", "00000", null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Should_Throw_When_NameIsBlank(string name)
    {
        Should.Throw<ArgumentException>(() =>
            Clinic.Create("C-1", name, "Addr", null, "City", "ST", "00000", null));
    }

    [Fact]
    public void Update_Should_MutateFields_And_StampUpdatedAt()
    {
        var clinic = Valid();

        clinic.Update("C-9", "Renamed", "9 Oak", "Suite 2", "Gotham", "NJ", "07001", "555-2000", isActive: false);

        clinic.Code.ShouldBe("C-9");
        clinic.Name.ShouldBe("Renamed");
        clinic.Address1.ShouldBe("9 Oak");
        clinic.Address2.ShouldBe("Suite 2");
        clinic.City.ShouldBe("Gotham");
        clinic.State.ShouldBe("NJ");
        clinic.Zip.ShouldBe("07001");
        clinic.Phone.ShouldBe("555-2000");
        clinic.IsActive.ShouldBeFalse();
        clinic.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_Should_SoftDelete_And_RecordWho()
    {
        var clinic = Valid();

        clinic.Delete("admin@tenant");

        clinic.IsDeleted.ShouldBeTrue();
        clinic.DeletedOnUtc.ShouldNotBeNull();
        clinic.DeletedBy.ShouldBe("admin@tenant");
    }

    [Fact]
    public void Create_Should_PreserveLegacyId_When_Provided()
    {
        var clinic = Clinic.Create("C-1", "Name", "Addr", null, "City", "ST", "00000", null, legacyId: 42);

        clinic.LegacyId.ShouldBe(42);
    }
}
