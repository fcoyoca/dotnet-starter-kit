using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.DeleteDiagnosticCategory;

public sealed class DeleteDiagnosticCategoryCommandValidator : AbstractValidator<DeleteDiagnosticCategoryCommand>
{
    public DeleteDiagnosticCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
