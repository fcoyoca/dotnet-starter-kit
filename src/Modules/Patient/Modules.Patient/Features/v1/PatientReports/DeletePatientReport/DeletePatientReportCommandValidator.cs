using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.DeletePatientReport;

public sealed class DeletePatientReportCommandValidator : AbstractValidator<DeletePatientReportCommand>
{
    public DeletePatientReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
    }
}
