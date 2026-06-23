using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.UpdateReportType;

public sealed class UpdateReportTypeCommandValidator : AbstractValidator<UpdateReportTypeCommand>
{
    public UpdateReportTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
