using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceTypes.DeleteInsuranceType;

public sealed class DeleteInsuranceTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteInsuranceTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteInsuranceTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.InsuranceType entity = await dbContext.InsuranceTypes
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance type {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
