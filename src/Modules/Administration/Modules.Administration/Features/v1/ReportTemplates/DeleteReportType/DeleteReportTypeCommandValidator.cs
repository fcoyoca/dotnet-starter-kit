using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.DeleteReportType;

public sealed class DeleteReportTypeCommandValidator : AbstractValidator<DeleteReportTypeCommand>
{
    public DeleteReportTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
