using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.CodeSources;

namespace FSH.Modules.Administration.Features.v1.CodeSources.CreateCodeSource;

public sealed class CreateCodeSourceCommandValidator : AbstractValidator<CreateCodeSourceCommand>
{
    public CreateCodeSourceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
