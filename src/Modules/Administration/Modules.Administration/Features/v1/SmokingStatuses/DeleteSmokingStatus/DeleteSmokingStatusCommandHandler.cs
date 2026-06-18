using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.SmokingStatuses;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.SmokingStatuses.DeleteSmokingStatus;

public sealed class DeleteSmokingStatusCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteSmokingStatusCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteSmokingStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        SmokingStatus entity = await dbContext.SmokingStatuses
            .FirstOrDefaultAsync(s => s.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"SmokingStatus {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
