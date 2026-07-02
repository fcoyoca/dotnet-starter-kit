using FluentValidation;
using FSH.Modules.Patient.Contracts.v1.Patients;

namespace FSH.Modules.Patient.Features.v1.Patients.CreatePatient;

public sealed class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.PatientCode).MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleInitial).MaximumLength(5);
        RuleFor(x => x.DateOfBirth).NotEmpty().LessThan(DateTime.UtcNow);
        RuleFor(x => x.Gender).NotEmpty().MaximumLength(10);
        RuleFor(x => x.MaritalStatus).MaximumLength(20);
        RuleFor(x => x.Email).MaximumLength(100).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.ZipCode).MaximumLength(10);
        RuleFor(x => x.Ssn).MaximumLength(11);
        RuleFor(x => x.MedicalAlertNotes).MaximumLength(8000);

        // Guardian required for minors
        When(x => x.IsMinor, () =>
        {
            RuleFor(x => x.GuardianFirstName).NotEmpty().WithMessage("Guardian first name is required for minor patients.");
            RuleFor(x => x.GuardianLastName).NotEmpty().WithMessage("Guardian last name is required for minor patients.");
        });
    }
}
