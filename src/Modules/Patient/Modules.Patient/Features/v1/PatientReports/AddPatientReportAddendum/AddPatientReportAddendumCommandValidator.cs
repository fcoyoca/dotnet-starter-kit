using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.AddPatientReportAddendum;

public sealed class AddPatientReportAddendumCommandValidator : AbstractValidator<AddPatientReportAddendumCommand>
{
    public AddPatientReportAddendumCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(16000);
    }
}
