using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategoryCodes;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategoryCodes.SetCodes;

public sealed class SetDiagnosticCategoryCodesCommandValidator : AbstractValidator<SetDiagnosticCategoryCodesCommand>
{
    public SetDiagnosticCategoryCodesCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.DiagnosticIds).NotNull();
        RuleForEach(x => x.DiagnosticIds).GreaterThan(0);
    }
}
