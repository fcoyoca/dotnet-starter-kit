using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.SetProcedures;

namespace Administration.Tests.Validators;

public sealed class SetInsuranceTypeProceduresCommandValidatorTests
{
    private readonly SetInsuranceTypeProceduresCommandValidator _sut = new();

    private static SetInsuranceTypeProceduresCommand Valid() => new(
        InsuranceTypeId: Guid.CreateVersion7(),
        Items:
        [
            new InsuranceTypeProcedureItem(Guid.CreateVersion7(), 20.00m),
            new InsuranceTypeProcedureItem(Guid.CreateVersion7(), 0m),
        ]);

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_For_EmptyItems()
    {
        _sut.TestValidate(Valid() with { Items = [] }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_InsuranceTypeIdEmpty()
    {
        _sut.TestValidate(Valid() with { InsuranceTypeId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.InsuranceTypeId);
    }

    [Fact]
    public void Validate_Should_Fail_When_ItemHasEmptyCode()
    {
        _sut.TestValidate(Valid() with { Items = [new InsuranceTypeProcedureItem(Guid.Empty, 10m)] })
            .ShouldHaveValidationErrorFor("Items[0].ProcedureCodeId");
    }

    [Fact]
    public void Validate_Should_Fail_When_ItemPriceNegative()
    {
        _sut.TestValidate(Valid() with { Items = [new InsuranceTypeProcedureItem(Guid.CreateVersion7(), -1m)] })
            .ShouldHaveValidationErrorFor("Items[0].Price");
    }
}
