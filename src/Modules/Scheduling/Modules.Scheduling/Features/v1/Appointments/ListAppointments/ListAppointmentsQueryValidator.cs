using FluentValidation;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.ListAppointments;

public sealed class ListAppointmentsQueryValidator : AbstractValidator<ListAppointmentsQuery>
{
    public ListAppointmentsQueryValidator()
    {
        RuleFor(x => x.ClinicId).NotEmpty();
        RuleFor(x => x.ToUtc).GreaterThan(x => x.FromUtc);
    }
}
