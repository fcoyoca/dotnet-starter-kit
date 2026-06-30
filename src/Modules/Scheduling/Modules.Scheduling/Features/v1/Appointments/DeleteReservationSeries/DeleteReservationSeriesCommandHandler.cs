using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.DeleteReservationSeries;

public sealed class DeleteReservationSeriesCommandHandler(SchedulingDbContext dbContext, AppointmentRealtimeNotifier notifier)
    : ICommandHandler<DeleteReservationSeriesCommand, int>
{
    public async ValueTask<int> Handle(DeleteReservationSeriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        List<Domain.Appointment> blocks = await dbContext.Appointments
            .Where(a => a.ReservationSeriesId == command.SeriesId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (blocks.Count == 0)
        {
            throw new NotFoundException($"Reservation series {command.SeriesId} not found.");
        }

        foreach (Domain.Appointment block in blocks)
        {
            block.Delete(deletedBy: null);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Domain.Appointment first = blocks[0];
        DateTime windowStart = blocks.Min(b => b.StartUtc);
        DateTime windowEnd = blocks.Max(b => b.EndUtc);
        await notifier.NotifyChangedAsync(first.ClinicId, first.ProviderId, windowStart, windowEnd, "deleted", cancellationToken)
            .ConfigureAwait(false);
        return blocks.Count;
    }
}
