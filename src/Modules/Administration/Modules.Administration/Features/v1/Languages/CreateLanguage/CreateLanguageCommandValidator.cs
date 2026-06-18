using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Languages;

namespace FSH.Modules.Administration.Features.v1.Languages.CreateLanguage;

public sealed class CreateLanguageCommandValidator : AbstractValidator<CreateLanguageCommand>
{
    public CreateLanguageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
