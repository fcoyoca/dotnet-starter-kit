using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.UpdateProcedureCategory;

public sealed class UpdateProcedureCategoryCommandValidator : AbstractValidator<UpdateProcedureCategoryCommand>
{
    public UpdateProcedureCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
