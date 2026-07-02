using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.Patients;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownAllergies;

public sealed class SetPatientNoKnownAllergiesCommandValidator : AbstractValidator<SetPatientNoKnownAllergiesCommand>
{
    public SetPatientNoKnownAllergiesCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
