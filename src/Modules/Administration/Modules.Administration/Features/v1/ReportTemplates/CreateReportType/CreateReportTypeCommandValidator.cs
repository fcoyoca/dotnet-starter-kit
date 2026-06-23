using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.ReportTemplates;

namespace FSH.Modules.Administration.Features.v1.ReportTemplates.CreateReportType;

public sealed class CreateReportTypeCommandValidator : AbstractValidator<CreateReportTypeCommand>
{
    public CreateReportTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
