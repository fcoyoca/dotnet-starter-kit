using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Macros;

namespace FSH.Modules.Administration.Features.v1.Macros.CreateMacro;

public sealed class CreateMacroCommandValidator : AbstractValidator<CreateMacroCommand>
{
    public CreateMacroCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Text).MaximumLength(8000);
    }
}
