using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Providers;

namespace FSH.Modules.Administration.Features.v1.Providers.UpdateProvider;

public sealed class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public UpdateProviderCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Prefix).MaximumLength(20);
        RuleFor(x => x.Suffix).MaximumLength(20);
        RuleFor(x => x.Specialty).MaximumLength(150);
        RuleFor(x => x.KareoExternalId).MaximumLength(64);
        RuleFor(x => x.UserId).MaximumLength(256);
        RuleFor(x => x.Npi)
            .Matches("^[0-9]{10}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Npi))
            .WithMessage("NPI must be exactly 10 digits.");
    }
}
