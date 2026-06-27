using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.CreateDiagnostic;

public sealed class CreateDiagnosticCommandValidator : AbstractValidator<CreateDiagnosticCommand>
{
    public CreateDiagnosticCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
        RuleFor(x => x.CodeSourceId).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.LongDescription).MaximumLength(1024);
    }
}
