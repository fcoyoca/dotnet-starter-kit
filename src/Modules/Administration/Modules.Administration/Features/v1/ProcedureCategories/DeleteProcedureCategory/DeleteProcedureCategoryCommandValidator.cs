using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.DeleteProcedureCategory;

public sealed class DeleteProcedureCategoryCommandValidator : AbstractValidator<DeleteProcedureCategoryCommand>
{
    public DeleteProcedureCategoryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
