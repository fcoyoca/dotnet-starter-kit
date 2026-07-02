using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientMedications;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientMedications.CreatePatientMedication;

public sealed class CreatePatientMedicationCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<CreatePatientMedicationCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientMedicationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        PatientMedication medication = PatientMedication.Create(
            command.PatientId, command.DrugName, command.RxAui, command.RxCode, command.Ndc,
            command.Prescriber, command.StartDate, command.EndDate,
            command.DoseValue, command.DoseUnitId, command.DosePeriodValue, command.DosePeriodUnit,
            command.Instructions, command.Indication, command.IsActive,
            currentUser.GetUserId().ToString(), currentUser.Name);

        dbContext.PatientMedications.Add(medication);

        if (command.IsActive && patient.HasNoKnownMedications)
        {
            patient.SetNoKnownMedications(false);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return medication.Id;
    }
}
