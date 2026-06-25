using FluentValidation;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientIncidents;

namespace FSH.Modules.Patient.Features.v1.PatientIncidents.UpdatePatientIncident;

public sealed class UpdatePatientIncidentCommandValidator : AbstractValidator<UpdatePatientIncidentCommand>
{
    public UpdatePatientIncidentCommandValidator()
    {
        RuleFor(x => x.IncidentId).NotEmpty();
        RuleFor(x => x.DateOfInitialVisit).NotEmpty();
        RuleFor(x => x.DateOfLoss).NotEmpty();
        RuleFor(x => x.AccidentState).MaximumLength(2);
        RuleFor(x => x.Comments).MaximumLength(8000);
        RuleFor(x => x.SummaryOfCare).MaximumLength(8000);

        When(x => x.IsAccident, () =>
        {
            RuleFor(x => x.AccidentType)
                .NotNull()
                .NotEqual(AccidentType.None)
                .WithMessage("AccidentType is required when IsAccident is true.");
        });

        When(x => x.AdherenceToPlan.HasValue, () =>
        {
            RuleFor(x => x.AdherenceToPlan!.Value).InclusiveBetween(0, 10);
        });
    }
}
