using FSH.Modules.Administration.Domain;

namespace Administration.Tests.Domain;

public sealed class InsuranceTypeProcedureTests
{
    [Fact]
    public void Create_Should_SetFields()
    {
        var typeId = Guid.CreateVersion7();
        var codeId = Guid.CreateVersion7();

        var link = InsuranceTypeProcedure.Create(typeId, codeId, 20.00m);

        link.Id.ShouldNotBe(Guid.Empty);
        link.InsuranceTypeId.ShouldBe(typeId);
        link.ProcedureCodeId.ShouldBe(codeId);
        link.Price.ShouldBe(20.00m);
        link.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_Should_ClampNegativePriceToZero()
    {
        var link = InsuranceTypeProcedure.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), -5m);
        link.Price.ShouldBe(0m);
    }

    [Fact]
    public void Create_Should_Throw_When_InsuranceTypeIdEmpty()
    {
        Should.Throw<ArgumentException>(() => InsuranceTypeProcedure.Create(Guid.Empty, Guid.CreateVersion7(), 1m));
    }

    [Fact]
    public void Create_Should_Throw_When_ProcedureCodeIdEmpty()
    {
        Should.Throw<ArgumentException>(() => InsuranceTypeProcedure.Create(Guid.CreateVersion7(), Guid.Empty, 1m));
    }

    [Fact]
    public void UpdatePrice_Should_MutatePrice_And_StampUpdatedAt()
    {
        var link = InsuranceTypeProcedure.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 10m);

        link.UpdatePrice(33.50m);

        link.Price.ShouldBe(33.50m);
        link.UpdatedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void UpdatePrice_Should_ClampNegativeToZero()
    {
        var link = InsuranceTypeProcedure.Create(Guid.CreateVersion7(), Guid.CreateVersion7(), 10m);
        link.UpdatePrice(-1m);
        link.Price.ShouldBe(0m);
    }
}
