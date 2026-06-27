using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.UpdateDiagnostic;

public sealed class UpdateDiagnosticCommandValidator : AbstractValidator<UpdateDiagnosticCommand>
{
    public UpdateDiagnosticCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(16);
        RuleFor(x => x.CodeSourceId).GreaterThan(0);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.LongDescription).MaximumLength(1024);
    }
}
