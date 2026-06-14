using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.Patients;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.Patients.RestorePatient;

public sealed class RestorePatientCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<RestorePatientCommand>
{
    public async ValueTask<Unit> Handle(RestorePatientCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var patient = await dbContext.Patients
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == command.PatientId && p.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Deleted patient {command.PatientId} not found.");

        patient.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
