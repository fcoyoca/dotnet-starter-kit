using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientReports;
using FSH.Modules.Patient.Data;
using FSH.Modules.Patient.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientReports.RequestReportReview;

public sealed class RequestReportReviewCommandHandler(PatientDbContext dbContext, ICurrentUser currentUser)
    : ICommandHandler<RequestReportReviewCommand, Unit>
{
    public async ValueTask<Unit> Handle(RequestReportReviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        PatientReport report = await dbContext.PatientReports
            .FirstOrDefaultAsync(x => x.Id == command.ReportId && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Report {command.ReportId} not found.");

        if (report.WorkflowStatus != ReportWorkflowStatus.Signed)
        {
            throw new CustomException("Only a signed report can be sent for review.", (IEnumerable<string>?)null, HttpStatusCode.Conflict);
        }

        report.RequestReview(currentUser.GetUserId().ToString(), command.ReviewerProviderId);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
