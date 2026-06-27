using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.PatientProblems;

namespace FSH.Modules.Patient.Features.v1.PatientProblems.DeletePatientProblem;

public sealed class DeletePatientProblemCommandValidator : AbstractValidator<DeletePatientProblemCommand>
{
    public DeletePatientProblemCommandValidator()
    {
        RuleFor(x => x.ProblemId).NotEmpty();
    }
}
