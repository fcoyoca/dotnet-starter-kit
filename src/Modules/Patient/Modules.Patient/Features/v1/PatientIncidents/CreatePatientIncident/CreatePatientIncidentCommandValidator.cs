using FluentValidation;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.CreatePatientIncident;

public sealed class CreatePatientIncidentCommandValidator : AbstractValidator<CreatePatientIncidentCommand>
{
    public CreatePatientIncidentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DateOfLoss).NotEmpty();
        RuleFor(x => x.AccidentState).MaximumLength(2);
        RuleFor(x => x.Comments).MaximumLength(8000);

        When(x => x.IsAccident, () =>
        {
            RuleFor(x => x.AccidentType)
                .NotNull()
                .NotEqual(AccidentType.None)
                .WithMessage("AccidentType is required when IsAccident is true.");
        });
    }
}
