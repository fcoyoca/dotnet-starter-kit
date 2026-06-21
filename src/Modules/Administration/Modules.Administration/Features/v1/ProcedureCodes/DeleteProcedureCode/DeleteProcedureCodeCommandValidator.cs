using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

namespace FSH.Modules.Administration.Features.v1.ProcedureCodes.DeleteProcedureCode;

public sealed class DeleteProcedureCodeCommandValidator : AbstractValidator<DeleteProcedureCodeCommand>
{
    public DeleteProcedureCodeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
