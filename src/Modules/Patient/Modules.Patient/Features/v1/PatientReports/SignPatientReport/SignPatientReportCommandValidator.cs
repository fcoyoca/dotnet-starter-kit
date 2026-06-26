using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SignPatientReport;

public sealed class SignPatientReportCommandValidator : AbstractValidator<SignPatientReportCommand>
{
    public SignPatientReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}
