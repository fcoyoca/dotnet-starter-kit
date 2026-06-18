using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Languages;

namespace FSH.Modules.Administration.Features.v1.Languages.UpdateLanguage;

public sealed class UpdateLanguageCommandValidator : AbstractValidator<UpdateLanguageCommand>
{
    public UpdateLanguageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
