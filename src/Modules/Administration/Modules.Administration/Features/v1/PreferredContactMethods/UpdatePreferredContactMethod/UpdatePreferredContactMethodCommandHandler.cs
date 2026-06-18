using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.PreferredContactMethods.UpdatePreferredContactMethod;

public sealed class UpdatePreferredContactMethodCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdatePreferredContactMethodCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePreferredContactMethodCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        PreferredContactMethod entity = await dbContext.PreferredContactMethods
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"PreferredContactMethod {command.Id} not found.");
        entity.Update(command.Name, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
