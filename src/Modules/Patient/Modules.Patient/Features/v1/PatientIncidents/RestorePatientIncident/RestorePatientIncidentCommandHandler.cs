using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.RestorePatientIncident;

public sealed class RestorePatientIncidentCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<RestorePatientIncidentCommand>
{
    public async ValueTask<Unit> Handle(RestorePatientIncidentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Bypass only the soft-delete filter so the deleted row is visible; tenant
        // scoping stays in force so a restore can't reach another tenant's incident.
        var incident = await dbContext.PatientIncidents
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(x => x.Id == command.IncidentId && x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Deleted incident {command.IncidentId} not found.");

        incident.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
