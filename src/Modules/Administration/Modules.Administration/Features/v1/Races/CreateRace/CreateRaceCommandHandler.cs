using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using FSH.Modules.Administration.Contracts.v1.Races;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Races.CreateRace;

public sealed class CreateRaceCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateRaceCommand, int>
{
    public async ValueTask<int> Handle(CreateRaceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Race entity = Race.Create(command.Name);
        dbContext.Races.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
