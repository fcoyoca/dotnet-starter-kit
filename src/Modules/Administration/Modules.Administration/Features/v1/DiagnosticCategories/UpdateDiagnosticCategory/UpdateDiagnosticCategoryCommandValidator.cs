using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.UpdateDiagnosticCategory;

public sealed class UpdateDiagnosticCategoryCommandValidator : AbstractValidator<UpdateDiagnosticCategoryCommand>
{
    public UpdateDiagnosticCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
