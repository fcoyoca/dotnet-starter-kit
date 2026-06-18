using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Races;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Races.DeleteRace;

public sealed class DeleteRaceCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteRaceCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteRaceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Race entity = await dbContext.Races
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Race {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
