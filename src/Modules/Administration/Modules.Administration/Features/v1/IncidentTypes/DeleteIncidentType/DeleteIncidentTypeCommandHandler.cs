using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.IncidentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.IncidentTypes.DeleteIncidentType;

public sealed class DeleteIncidentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteIncidentTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteIncidentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.IncidentType entity = await dbContext.IncidentTypes
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Incident type {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
