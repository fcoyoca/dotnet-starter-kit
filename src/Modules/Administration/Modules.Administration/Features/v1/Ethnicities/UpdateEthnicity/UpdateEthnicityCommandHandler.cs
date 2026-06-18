using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Ethnicities;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Ethnicities.UpdateEthnicity;

public sealed class UpdateEthnicityCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateEthnicityCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateEthnicityCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Ethnicity entity = await dbContext.Ethnicities
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Ethnicity {command.Id} not found.");
        entity.Update(command.Name, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
