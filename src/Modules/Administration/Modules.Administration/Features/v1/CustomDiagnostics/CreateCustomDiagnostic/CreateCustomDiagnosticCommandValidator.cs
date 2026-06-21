using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.CreateCustomDiagnostic;

public sealed class CreateCustomDiagnosticCommandValidator : AbstractValidator<CreateCustomDiagnosticCommand>
{
    public CreateCustomDiagnosticCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.LongDescription).MaximumLength(2000);
    }
}
