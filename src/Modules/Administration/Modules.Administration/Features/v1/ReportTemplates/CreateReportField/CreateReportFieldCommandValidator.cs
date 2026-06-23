using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportField;

public sealed class CreateReportFieldCommandValidator : AbstractValidator<CreateReportFieldCommand>
{
    public CreateReportFieldCommandValidator()
    {
        RuleFor(x => x.ReportTypeId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Category).MaximumLength(128);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
