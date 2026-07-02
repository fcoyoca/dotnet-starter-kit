using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.Patients;

namespace FSH.Modules.Patient.Features.v1.Patients.SetPatientNoKnownMedications;

public sealed class SetPatientNoKnownMedicationsCommandValidator : AbstractValidator<SetPatientNoKnownMedicationsCommand>
{
    public SetPatientNoKnownMedicationsCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}
