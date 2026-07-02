using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.CreateAllergyReaction;

public sealed class CreateAllergyReactionCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateAllergyReactionCommand, int>
{
    public async ValueTask<int> Handle(CreateAllergyReactionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        AllergyReaction entity = AllergyReaction.Create(command.Term, command.SnomedCode);
        dbContext.AllergyReactions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
