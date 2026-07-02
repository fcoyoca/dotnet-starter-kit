using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;

public sealed class SetPatientNoKnownAllergiesCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<SetPatientNoKnownAllergiesCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetPatientNoKnownAllergiesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Patient patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        if (command.Value)
        {
            bool hasActive = await dbContext.PatientAllergies
                .AnyAsync(a => a.PatientId == command.PatientId && a.IsActive, cancellationToken)
                .ConfigureAwait(false);
            if (hasActive)
            {
                throw new CustomException(
                    "You cannot set No Allergies when Active allergies exist.",
                    (IEnumerable<string>?)null,
                    HttpStatusCode.Conflict);
            }
        }

        patient.SetNoKnownAllergies(command.Value);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
