using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Macros;

namespace FSH.Modules.Administration.Features.v1.Macros.UpdateMacro;

public sealed class UpdateMacroCommandValidator : AbstractValidator<UpdateMacroCommand>
{
    public UpdateMacroCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Text).MaximumLength(8000);
    }
}
