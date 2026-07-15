using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.RestorePatientReport;

public sealed class RestorePatientReportCommandValidator : AbstractValidator<RestorePatientReportCommand>
{
    public RestorePatientReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}
