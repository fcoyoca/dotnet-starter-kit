using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Languages;

namespace FSH.Modules.Administration.Features.v1.Languages.DeleteLanguage;

public sealed class DeleteLanguageCommandValidator : AbstractValidator<DeleteLanguageCommand>
{
    public DeleteLanguageCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
