using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.DeletePreferredContactMethod;

public sealed class DeletePreferredContactMethodCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeletePreferredContactMethodCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeletePreferredContactMethodCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        PreferredContactMethod entity = await dbContext.PreferredContactMethods
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"PreferredContactMethod {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
