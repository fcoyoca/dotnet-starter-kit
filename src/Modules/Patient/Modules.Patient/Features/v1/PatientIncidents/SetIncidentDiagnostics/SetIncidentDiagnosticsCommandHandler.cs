using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.SetIncidentDiagnostics;

public sealed class SetIncidentDiagnosticsCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<SetIncidentDiagnosticsCommand, Unit>
{
    public async ValueTask<Unit> Handle(SetIncidentDiagnosticsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientIncident incident = await dbContext.PatientIncidents
            .Include(x => x.Diagnostics)
            .FirstOrDefaultAsync(x => x.Id == command.IncidentId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Incident {command.IncidentId} not found.");

        incident.SetDiagnostics(command.DiagnosticIds.Distinct().ToList());

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
