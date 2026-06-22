using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Macros;

namespace FSH.Modules.Administration.Features.v1.Macros.DeleteMacro;

public sealed class DeleteMacroCommandValidator : AbstractValidator<DeleteMacroCommand>
{
    public DeleteMacroCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
