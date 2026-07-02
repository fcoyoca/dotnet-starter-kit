using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.CreateMedicationDoseUnit;

public sealed class CreateMedicationDoseUnitCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateMedicationDoseUnitCommand, int>
{
    public async ValueTask<int> Handle(CreateMedicationDoseUnitCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        MedicationDoseUnit entity = MedicationDoseUnit.Create(command.Name);
        dbContext.MedicationDoseUnits.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
