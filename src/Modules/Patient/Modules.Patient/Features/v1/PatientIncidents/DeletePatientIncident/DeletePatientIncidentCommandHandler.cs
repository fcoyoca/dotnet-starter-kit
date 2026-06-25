using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.DeletePatientIncident;

public sealed class DeletePatientIncidentCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<DeletePatientIncidentCommand>
{
    public async ValueTask<Unit> Handle(DeletePatientIncidentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var incident = await dbContext.PatientIncidents
            .FirstOrDefaultAsync(x => x.Id == command.IncidentId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Incident {command.IncidentId} not found.");

        incident.Delete(null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
