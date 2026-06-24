using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.DeleteAppointment;

public sealed class DeleteAppointmentCommandValidator : AbstractValidator<DeleteAppointmentCommand>
{
    public DeleteAppointmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
