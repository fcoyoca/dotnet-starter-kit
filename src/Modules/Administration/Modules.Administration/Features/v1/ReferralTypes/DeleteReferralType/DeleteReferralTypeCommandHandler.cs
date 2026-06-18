using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.ReferralTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.ReferralTypes.DeleteReferralType;

public sealed class DeleteReferralTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteReferralTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteReferralTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ReferralType entity = await dbContext.ReferralTypes
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ReferralType {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
