using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.UpdatePatientMedication;

public sealed class UpdatePatientMedicationCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<UpdatePatientMedicationCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientMedicationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientMedication medication = await dbContext.PatientMedications
            .FirstOrDefaultAsync(m => m.Id == command.MedicationId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Medication {command.MedicationId} not found.");

        medication.Update(
            command.DrugName, command.RxAui, command.RxCode, command.Ndc,
            command.Prescriber, command.StartDate, command.EndDate,
            command.DoseValue, command.DoseUnitId, command.DosePeriodValue, command.DosePeriodUnit,
            command.Instructions, command.Indication, command.IsActive,
            currentUser.GetUserId().ToString(), currentUser.Name);

        if (command.IsActive)
        {
            Domain.Patient? patient = await dbContext.Patients
                .FirstOrDefaultAsync(p => p.Id == medication.PatientId, cancellationToken)
                .ConfigureAwait(false);
            if (patient is not null && patient.HasNoKnownMedications)
            {
                patient.SetNoKnownMedications(false);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
