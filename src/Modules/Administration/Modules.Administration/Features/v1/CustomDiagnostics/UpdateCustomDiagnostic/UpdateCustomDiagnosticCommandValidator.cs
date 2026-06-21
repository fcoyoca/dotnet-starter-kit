using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.UpdateCustomDiagnostic;

public sealed class UpdateCustomDiagnosticCommandValidator : AbstractValidator<UpdateCustomDiagnosticCommand>
{
    public UpdateCustomDiagnosticCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.LongDescription).MaximumLength(2000);
    }
}
