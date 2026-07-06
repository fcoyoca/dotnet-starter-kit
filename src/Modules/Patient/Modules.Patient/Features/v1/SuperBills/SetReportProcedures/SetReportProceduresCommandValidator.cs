using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.SuperBills;

namespace FSH.Modules.Patient.Features.v1.SuperBills.SetReportProcedures;

public sealed class SetReportProceduresCommandValidator : AbstractValidator<SetReportProceduresCommand>
{
    public SetReportProceduresCommandValidator()
    {
        RuleFor(x => x.ReportId).NotEmpty();
        RuleFor(x => x.Procedures).NotNull();
        RuleForEach(x => x.Procedures).ChildRules(procedure =>
        {
            procedure.RuleFor(p => p.ProcedureCodeId).NotEmpty();
            procedure.RuleFor(p => p.Code).NotEmpty().MaximumLength(32);
            procedure.RuleFor(p => p.Description).MaximumLength(512);
            procedure.RuleFor(p => p.Charge).GreaterThanOrEqualTo(0);
            procedure.RuleFor(p => p.DiagnosticIds)
                .NotEmpty()
                .WithMessage("Each procedure must be linked to at least one diagnostic.");
            procedure.RuleForEach(p => p.DiagnosticIds).NotEmpty();
        });
    }
}
