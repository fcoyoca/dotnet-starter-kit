using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.NoShowAppointment;

public sealed class NoShowAppointmentCommandValidator : AbstractValidator<NoShowAppointmentCommand>
{
    public NoShowAppointmentCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
