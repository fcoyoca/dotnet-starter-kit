using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CheckOutAppointment;

public sealed class CheckOutAppointmentCommandValidator : AbstractValidator<CheckOutAppointmentCommand>
{
    public CheckOutAppointmentCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
