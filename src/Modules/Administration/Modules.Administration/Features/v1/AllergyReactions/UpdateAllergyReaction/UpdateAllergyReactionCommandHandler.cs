using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.AllergyReactions;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.AllergyReactions.UpdateAllergyReaction;

public sealed class UpdateAllergyReactionCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateAllergyReactionCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateAllergyReactionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        AllergyReaction entity = await dbContext.AllergyReactions
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"AllergyReaction {command.Id} not found.");
        entity.Update(command.Term, command.IsActive, command.SnomedCode);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
