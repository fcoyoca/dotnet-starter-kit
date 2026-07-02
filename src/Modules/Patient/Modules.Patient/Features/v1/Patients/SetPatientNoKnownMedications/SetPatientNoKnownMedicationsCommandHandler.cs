using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownMedications;

public sealed class SetPatientNoKnownMedicationsCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<SetPatientNoKnownMedicationsCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetPatientNoKnownMedicationsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        if (command.Value)
        {
            bool hasActive = await dbContext.PatientMedications
                .AnyAsync(m => m.PatientId == command.PatientId && m.IsActive, cancellationToken)
                .ConfigureAwait(false);
            if (hasActive)
            {
                throw new CustomException(
                    "You cannot set No Medications when Active medications exist.",
                    (IEnumerable<string>?)null,
                    HttpStatusCode.Conflict);
            }
        }

        patient.SetNoKnownMedications(command.Value);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
