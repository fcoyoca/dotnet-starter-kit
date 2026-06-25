using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CreateAppointment;

public sealed class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentCommandValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.EndUtc).GreaterThan(x => x.StartUtc);
        RuleFor(x => x.Notes).MaximumLength(4000);
        RuleFor(x => x.ReservationTitle).MaximumLength(200);
        RuleFor(x => x.ReservationTitle)
            .NotEmpty()
            .When(x => x.IsReservation)
            .WithMessage("Reservation title is required for a reserve-time block.");
    }
}
