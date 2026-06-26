using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.CreatePatientReport;

public sealed class CreatePatientReportCommandValidator : AbstractValidator<CreatePatientReportCommand>
{
    public CreatePatientReportCommandValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ReportTypeId).GreaterThan(0);
        RuleFor(x => x.ReportDate).NotEmpty();
    }
}
