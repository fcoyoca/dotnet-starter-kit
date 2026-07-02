using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.UpdateAllergyReaction;

public sealed class UpdateAllergyReactionCommandValidator : AbstractValidator<UpdateAllergyReactionCommand>
{
    public UpdateAllergyReactionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Term).NotEmpty().MaximumLength(256);
        RuleFor(x => x.SnomedCode).MaximumLength(32);
    }
}
