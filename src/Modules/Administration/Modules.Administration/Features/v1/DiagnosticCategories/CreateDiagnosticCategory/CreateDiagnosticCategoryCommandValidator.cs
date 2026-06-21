using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.CreateDiagnosticCategory;

public sealed class CreateDiagnosticCategoryCommandValidator : AbstractValidator<CreateDiagnosticCategoryCommand>
{
    public CreateDiagnosticCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
