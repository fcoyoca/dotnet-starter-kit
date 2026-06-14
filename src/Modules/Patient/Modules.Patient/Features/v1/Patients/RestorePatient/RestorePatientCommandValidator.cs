using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.Patients;

namespace FSH.Modules.Patient.Features.v1.Patients.RestorePatient;

public sealed class RestorePatientCommandValidator : AbstractValidator<RestorePatientCommand>
{
    public RestorePatientCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
