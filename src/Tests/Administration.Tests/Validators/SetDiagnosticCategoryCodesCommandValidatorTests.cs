using FluentValidation.TestHelper;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;
using FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.SetCodes;

namespace Administration.Tests.Validators;

public sealed class SetDiagnosticCategoryCodesCommandValidatorTests
{
    private readonly SetDiagnosticCategoryCodesCommandValidator _sut = new();

    private static SetDiagnosticCategoryCodesCommand Valid() => new(
        CategoryId: Guid.CreateVersion7(),
        DiagnosticIds: [1, 2, 3]);

    [Fact]
    public void Validate_Should_Pass_For_ValidCommand()
    {
        _sut.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Pass_For_EmptyList()
    {
        _sut.TestValidate(Valid() with { DiagnosticIds = [] }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_CategoryIdEmpty()
    {
        _sut.TestValidate(Valid() with { CategoryId = Guid.Empty })
            .ShouldHaveValidationErrorFor(x => x.CategoryId);
    }

    [Fact]
    public void Validate_Should_Fail_When_DiagnosticIdsNull()
    {
        _sut.TestValidate(Valid() with { DiagnosticIds = null! })
            .ShouldHaveValidationErrorFor(x => x.DiagnosticIds);
    }

    [Fact]
    public void Validate_Should_Fail_When_DiagnosticIdNonPositive()
    {
        _sut.TestValidate(Valid() with { DiagnosticIds = [0] })
            .ShouldHaveValidationErrorFor("DiagnosticIds[0]");
    }

    [Fact]
    public void Validate_Should_Fail_When_DiagnosticIdNegative()
    {
        _sut.TestValidate(Valid() with { DiagnosticIds = [-1] })
            .ShouldHaveValidationErrorFor("DiagnosticIds[0]");
    }
}
