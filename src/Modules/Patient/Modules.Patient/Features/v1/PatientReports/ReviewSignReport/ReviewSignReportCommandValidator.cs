using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.ReviewSignReport;

public sealed class ReviewSignReportCommandValidator : AbstractValidator<ReviewSignReportCommand>
{
    public ReviewSignReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}
