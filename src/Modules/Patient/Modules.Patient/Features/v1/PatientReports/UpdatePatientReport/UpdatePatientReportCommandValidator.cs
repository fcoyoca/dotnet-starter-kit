using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.UpdatePatientReport;

public sealed class UpdatePatientReportCommandValidator : AbstractValidator<UpdatePatientReportCommand>
{
    public UpdatePatientReportCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ReportDate).NotEmpty();
        RuleFor(x => x.Vitals).NotNull();
        RuleFor(x => x.FieldValues).NotNull();
    }
}
