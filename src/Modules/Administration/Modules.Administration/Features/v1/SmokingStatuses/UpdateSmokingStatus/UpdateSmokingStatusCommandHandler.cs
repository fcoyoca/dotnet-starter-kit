using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.UpdateSmokingStatus;

public sealed class UpdateSmokingStatusCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateSmokingStatusCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateSmokingStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        SmokingStatus entity = await dbContext.SmokingStatuses
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"SmokingStatus {command.Id} not found.");
        entity.Update(command.Name, command.IsActive, command.SnomedCode);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
