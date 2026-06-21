using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Providers;

namespace FSH.Modules.Administration.Features.v1.Providers.DeleteProvider;

public sealed class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public DeleteProviderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
