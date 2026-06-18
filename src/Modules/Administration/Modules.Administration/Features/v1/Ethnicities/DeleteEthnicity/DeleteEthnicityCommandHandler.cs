using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.DeleteEthnicity;

public sealed class DeleteEthnicityCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteEthnicityCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteEthnicityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Ethnicity entity = await dbContext.Ethnicities
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Ethnicity {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
