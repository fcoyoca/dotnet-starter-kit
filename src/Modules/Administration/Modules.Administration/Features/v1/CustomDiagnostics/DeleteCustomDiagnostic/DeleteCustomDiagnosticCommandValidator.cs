using FluentValidation;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.DeleteCustomDiagnostic;

public sealed class DeleteCustomDiagnosticCommandValidator : AbstractValidator<DeleteCustomDiagnosticCommand>
{
    public DeleteCustomDiagnosticCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
