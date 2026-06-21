using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.CreateProcedureCategory;

public sealed class CreateProcedureCategoryCommandValidator : AbstractValidator<CreateProcedureCategoryCommand>
{
    public CreateProcedureCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
