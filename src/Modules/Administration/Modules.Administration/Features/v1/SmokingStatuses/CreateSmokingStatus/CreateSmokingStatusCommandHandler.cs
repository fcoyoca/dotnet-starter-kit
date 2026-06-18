using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.CreateSmokingStatus;

public sealed class CreateSmokingStatusCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateSmokingStatusCommand, int>
{
    public async ValueTask<int> Handle(CreateSmokingStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        SmokingStatus entity = SmokingStatus.Create(command.Name, command.SnomedCode);
        dbContext.SmokingStatuses.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
