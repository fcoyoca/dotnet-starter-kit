using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.Patients;

namespace FSH.Modules.Patient.Features.v1.Patients.DeletePatient;

public sealed class DeletePatientCommandValidator : AbstractValidator<DeletePatientCommand>
{
    public DeletePatientCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
