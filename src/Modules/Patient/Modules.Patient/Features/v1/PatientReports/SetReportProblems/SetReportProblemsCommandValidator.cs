using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.SetReportProblems;

public sealed class SetReportProblemsCommandValidator : AbstractValidator<SetReportProblemsCommand>
{
    public SetReportProblemsCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.ProblemIds).NotNull();
    }
}
