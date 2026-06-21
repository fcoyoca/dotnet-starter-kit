using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypeProcedures;
using FSH.Modules.Administration.Features.v1.InsuranceTypeProcedures.AssociateProcedure;

namespace Administration.Tests.Validators;

public sealed class AssociateProcedureToInsuranceTypeCommandValidatorTests
{
    private readonly AssociateProcedureToInsuranceTypeCommandValidator _sut = new();

    private static AssociateProcedureToInsuranceTypeCommand Valid() => new(
        InsuranceTypeId: Guid.CreateVersion7(),
        ProcedureCodeId: Guid.CreateVersion7(),
        Price: 20.00m);

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_InsuranceTypeIdEmpty()
    {
        _sut.TestValidate(Valid() with { InsuranceTypeId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.InsuranceTypeId);
    }

    [Fact]
    public void Validate_Should_Fail_When_ProcedureCodeIdEmpty()
    {
        _sut.TestValidate(Valid() with { ProcedureCodeId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.ProcedureCodeId);
    }

    [Fact]
    public void Validate_Should_Fail_When_PriceNegative()
    {
        _sut.TestValidate(Valid() with { Price = -1m })
            .ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_Should_Pass_When_PriceZero()
    {
        _sut.TestValidate(Valid() with { Price = 0m }).ShouldNotHaveAnyValidationErrors();
    }
}
