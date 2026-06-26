using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.AddPatientReportAddendum;

public sealed class AddPatientReportAddendumCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<AddPatientReportAddendumCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddPatientReportAddendumCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .Include(x => x.Addendums)
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        PatientReportAddendum addendum = report.AddAddendum(currentUser.GetUserId().ToString(), currentUser.Name, command.Text);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return addendum.Id;
    }
}
