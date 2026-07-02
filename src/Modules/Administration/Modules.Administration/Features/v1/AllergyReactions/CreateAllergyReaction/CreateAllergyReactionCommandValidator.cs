using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.CreateAllergyReaction;

public sealed class CreateAllergyReactionCommandValidator : AbstractValidator<CreateAllergyReactionCommand>
{
    public CreateAllergyReactionCommandValidator()
    {
        RuleFor(x => x.Term).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SnomedCode).MaximumLength(32);
    }
}
