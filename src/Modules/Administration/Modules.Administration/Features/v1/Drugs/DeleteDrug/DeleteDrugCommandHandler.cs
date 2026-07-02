using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Drugs.DeleteDrug;

public sealed class DeleteDrugCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteDrugCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteDrugCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Drug drug = await dbContext.Drugs
            .FirstOrDefaultAsync(d => d.Id == command.Id && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Drug {command.Id} not found.");

        drug.Delete(null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
