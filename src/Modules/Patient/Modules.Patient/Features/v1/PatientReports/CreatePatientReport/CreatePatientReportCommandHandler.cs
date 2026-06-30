using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.CreatePatientReport;

public sealed class CreatePatientReportCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<CreatePatientReportCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePatientReportCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool incidentExists = await dbContext.PatientIncidents
            .AnyAsync(i => i.Id == command.IncidentId && !i.IsDeleted, cancellationToken)
            .ConfigureAwait(false);
        if (!incidentExists)
        {
            throw new NotFoundException($"Incident {command.IncidentId} not found.");
        }

        PatientReport report = PatientReport.Create(
            command.IncidentId,
            command.PatientId,
            command.ReportTypeId,
            command.ReportDate,
            command.ProviderId,
            command.ClinicId,
            command.IsNoShow,
            command.AppointmentId);

        dbContext.PatientReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return report.Id;
    }
}
