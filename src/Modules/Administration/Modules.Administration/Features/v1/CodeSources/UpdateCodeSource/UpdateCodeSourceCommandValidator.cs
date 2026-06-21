using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.CodeSources;

namespace FSH.Modules.Administration.Features.v1.CodeSources.UpdateCodeSource;

public sealed class UpdateCodeSourceCommandValidator : AbstractValidator<UpdateCodeSourceCommand>
{
    public UpdateCodeSourceCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}
