using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.UpdateReferralType;

public sealed class UpdateReferralTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateReferralTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateReferralTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ReferralType entity = await dbContext.ReferralTypes
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ReferralType {command.Id} not found.");
        entity.Update(command.Name, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
