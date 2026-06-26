using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.RequestReportReview;

public sealed class RequestReportReviewCommandValidator : AbstractValidator<RequestReportReviewCommand>
{
    public RequestReportReviewCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ReviewerProviderId).NotEmpty();
    }
}
