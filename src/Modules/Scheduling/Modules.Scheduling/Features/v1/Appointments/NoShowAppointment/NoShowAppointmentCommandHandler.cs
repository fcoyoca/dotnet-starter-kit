using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.NoShowAppointment;

public sealed class NoShowAppointmentCommandHandler(SchedulingDbContext dbContext, AppointmentRealtimeNotifier notifier)
    : ICommandHandler<NoShowAppointmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(NoShowAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Appointment entity = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment {command.Id} not found.");

        entity.MarkNoShow();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await notifier.NotifyChangedAsync(entity.ClinicId, entity.ProviderId, entity.StartUtc, entity.EndUtc, "no-show", cancellationToken)
            .ConfigureAwait(false);
        return Unit.Value;
    }
}
