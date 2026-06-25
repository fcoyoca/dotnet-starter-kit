using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CheckOutAppointment;

public sealed class CheckOutAppointmentCommandHandler(SchedulingDbContext dbContext, AppointmentRealtimeNotifier notifier)
    : ICommandHandler<CheckOutAppointmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(CheckOutAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Appointment entity = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment {command.Id} not found.");

        entity.CheckOut();
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await notifier.NotifyChangedAsync(entity.ClinicId, entity.ProviderId, entity.StartUtc, entity.EndUtc, "checked-out", cancellationToken)
            .ConfigureAwait(false);
        return Unit.Value;
    }
}
