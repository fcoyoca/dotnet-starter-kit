using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.ExportPatientReportsPdf;

public sealed class ExportPatientReportsPdfQueryValidator : AbstractValidator<ExportPatientReportsPdfQuery>
{
    public const int MaxReportsPerExport = 50;

    public ExportPatientReportsPdfQueryValidator()
    {
        RuleFor(x => x.ReportIds)
            .NotEmpty().WithMessage("Select at least one report to export.")
            .Must(ids => ids.Count <= MaxReportsPerExport)
            .WithMessage($"A single export is limited to {MaxReportsPerExport} reports.");
        RuleForEach(x => x.ReportIds).NotEmpty();
    }
}
