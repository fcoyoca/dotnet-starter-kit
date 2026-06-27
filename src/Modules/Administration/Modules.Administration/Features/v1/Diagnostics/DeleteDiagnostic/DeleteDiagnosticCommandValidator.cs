using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.DeleteDiagnostic;

public sealed class DeleteDiagnosticCommandValidator : AbstractValidator<DeleteDiagnosticCommand>
{
    public DeleteDiagnosticCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
    }
}
