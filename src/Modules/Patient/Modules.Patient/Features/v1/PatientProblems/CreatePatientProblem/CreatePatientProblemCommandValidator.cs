using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.CreatePatientProblem;

public sealed class CreatePatientProblemCommandValidator : AbstractValidator<CreatePatientProblemCommand>
{
    public CreatePatientProblemCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DiagnosticId).NotEmpty();
        RuleFor(x => x.DiagnosticCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(4000);
    }
}
