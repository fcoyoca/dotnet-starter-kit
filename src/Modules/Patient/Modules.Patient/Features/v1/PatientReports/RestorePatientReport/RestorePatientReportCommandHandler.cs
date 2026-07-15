using FSH.Framework.Core.Exceptions;
using FSH.Framework.Persistence;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.RestorePatientReport;

public sealed class RestorePatientReportCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<RestorePatientReportCommand, Unit>
{
    public async ValueTask<Unit> Handle(RestorePatientReportCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Bypass only the soft-delete filter so the deleted row is visible; tenant
        // scoping stays in force so a restore can't reach another tenant's report.
        PatientReport report = await dbContext.PatientReports
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Deleted report {command.ReportId} not found.");

        report.Restore();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
