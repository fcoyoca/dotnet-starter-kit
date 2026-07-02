using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.DeleteMedicationDoseUnit;

public sealed class DeleteMedicationDoseUnitCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteMedicationDoseUnitCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteMedicationDoseUnitCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        MedicationDoseUnit entity = await dbContext.MedicationDoseUnits
            .FirstOrDefaultAsync(m => m.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"MedicationDoseUnit {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
