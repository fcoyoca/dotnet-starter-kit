using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportField;

public sealed class DeleteReportFieldCommandValidator : AbstractValidator<DeleteReportFieldCommand>
{
    public DeleteReportFieldCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
