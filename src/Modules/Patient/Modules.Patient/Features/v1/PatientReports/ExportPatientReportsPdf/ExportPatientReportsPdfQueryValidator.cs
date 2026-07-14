using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientReports;

namespace FSH.Modules.Patient.Features.v1.PatientReports.ExportPatientReportsPdf;

public sealed class ExportPatientReportsPdfQueryValidator : AbstractValidator<ExportPatientReportsPdfQuery>
{
    public const int MaxReportsPerExport = 50;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    public ExportPatientReportsPdfQueryValidator()
    {
        RuleFor(x => x.ReportIds)
            .NotEmpty().WithMessage("Select at least one report to export.")
            .Must(ids => ids.Count <= MaxReportsPerExport)
            .WithMessage($"A single export is limited to {MaxReportsPerExport} reports.");
        RuleForEach(x => x.ReportIds).NotEmpty();

        // Omitted entirely = a plain, unencrypted export. Supplied = it must actually protect
        // something, so an empty or trivially short password is rejected rather than silently
        // producing a "secure" download anyone can open.
        RuleFor(x => x.Password!)
            .NotEmpty().WithMessage("Enter a password for the protected download.")
            .MinimumLength(MinPasswordLength)
            .WithMessage($"The password must be at least {MinPasswordLength} characters.")
            .MaximumLength(MaxPasswordLength)
            .When(x => x.Password is not null);
    }
}
