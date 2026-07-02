using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.DeleteAllergyReaction;

public sealed class DeleteAllergyReactionCommandValidator : AbstractValidator<DeleteAllergyReactionCommand>
{
    public DeleteAllergyReactionCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
