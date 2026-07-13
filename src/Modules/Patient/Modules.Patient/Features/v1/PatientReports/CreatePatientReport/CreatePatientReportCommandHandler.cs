using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.CreatePatientReport;

public sealed class CreatePatientReportCommandHandler(PatientDbContext dbContext, IMediator mediator)
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

        // Stamp each field with its boilerplate, so a new report opens already
        // filled in (legacy rfDefaultText). Applied here and only here: once the
        // report exists its field text is the report's own, and a later edit to a
        // default must not rewrite it. Report fields live in Administration —
        // cross-module read via Contracts.
        IReadOnlyList<ReportFieldDto> fields = await mediator
            .Send(new ListReportFieldsQuery(command.ReportTypeId), cancellationToken)
            .ConfigureAwait(false);

        List<(int FieldId, string Text)> defaults = fields
            .Where(f => !string.IsNullOrWhiteSpace(f.DefaultText))
            .Select(f => (f.Id, Text: f.DefaultText!))
            .ToList();

        if (defaults.Count > 0)
        {
            report.SetFieldValues(defaults);
        }

        dbContext.PatientReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return report.Id;
    }
}
