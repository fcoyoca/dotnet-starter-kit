using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FSH.Modules.Patient.Features.v1.PatientReports.UpdatePatientReport;

public sealed class UpdatePatientReportCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<UpdatePatientReportCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdatePatientReportCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .Include(x => x.FieldValues)
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        if (report.IsSigned)
        {
            throw new CustomException(
                "A signed report cannot be edited. Add an addendum instead.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        report.UpdateHeader(command.ReportDate, command.ProviderId, command.ClinicId, command.IsNoShow);
        report.SetVitals(new PatientReportVitals(
            command.Vitals.HeightInches, command.Vitals.WeightLbs, command.Vitals.Bmi,
            command.Vitals.Systolic, command.Vitals.Diastolic, command.Vitals.Pulse, command.Vitals.TemperatureF));
        report.SetFieldValues(command.FieldValues.Select(f => (f.ReportFieldId, f.Text)));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
