using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Drugs;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Drugs.UpdateDrug;

public sealed class UpdateDrugCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateDrugCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateDrugCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Drug drug = await dbContext.Drugs
            .FirstOrDefaultAsync(d => d.Id == command.Id && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Drug {command.Id} not found.");

        drug.Update(
            command.Name,
            command.RxAui,
            command.RxCui,
            command.Tty,
            command.Sab,
            command.Code,
            command.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
