using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.UpdateProcedureCode;

public sealed class UpdateProcedureCodeCommandValidator : AbstractValidator<UpdateProcedureCodeCommand>
{
    public UpdateProcedureCodeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ProcedureCategoryId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.CodeSource).MaximumLength(50);
        RuleFor(x => x.MacroText).MaximumLength(4000);
    }
}
