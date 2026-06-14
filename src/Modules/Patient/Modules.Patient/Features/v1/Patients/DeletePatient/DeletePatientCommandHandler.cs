using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.DeletePatient;

public sealed class DeletePatientCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<DeletePatientCommand>
{
    public async ValueTask<Unit> Handle(DeletePatientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var patient = await dbContext.Patients
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Patient {command.PatientId} not found.");

        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
