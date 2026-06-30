using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CreateRecurringReservation;

public sealed class CreateRecurringReservationCommandHandler(
    SchedulingDbContext dbContext, IMediator mediator, AppointmentRealtimeNotifier notifier)
    : ICommandHandler<CreateRecurringReservationCommand, int>
{
    public async ValueTask<int> Handle(CreateRecurringReservationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await AppointmentRefValidator.EnsureRefsExistAsync(
            mediator, command.ClinicId, command.ProviderId, appointmentTypeId: null, cancellationToken)
            .ConfigureAwait(false);

        Guid seriesId = Guid.CreateVersion7();
        var blocks = command.Occurrences
            .Select(o => Domain.Appointment.Create(
                command.ClinicId, command.ProviderId, patientId: null, appointmentTypeId: null,
                o.StartUtc, o.EndUtc, command.Notes,
                isReservation: true, reservationTitle: command.Title, reservationSeriesId: seriesId))
            .ToList();

        await dbContext.Appointments.AddRangeAsync(blocks, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        DateTime windowStart = blocks.Min(b => b.StartUtc);
        DateTime windowEnd = blocks.Max(b => b.EndUtc);
        await notifier.NotifyChangedAsync(command.ClinicId, command.ProviderId, windowStart, windowEnd, "created", cancellationToken)
            .ConfigureAwait(false);
        return blocks.Count;
    }
}
