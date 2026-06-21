using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.CodeSources;

namespace FSH.Modules.Administration.Features.v1.CodeSources.DeleteCodeSource;

public sealed class DeleteCodeSourceCommandValidator : AbstractValidator<DeleteCodeSourceCommand>
{
    public DeleteCodeSourceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
