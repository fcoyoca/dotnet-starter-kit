using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.RescheduleAppointment;

public sealed class RescheduleAppointmentCommandHandler(
    SchedulingDbContext dbContext, IMediator mediator, AppointmentRealtimeNotifier notifier)
    : ICommandHandler<RescheduleAppointmentCommand, Guid>
{
    public async ValueTask<Guid> Handle(RescheduleAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Appointment original = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment {command.Id} not found.");

        if (original.IsReservation)
        {
            throw new CustomException("Reserve-time blocks cannot be rescheduled.");
        }

        if (original.Cancelled)
        {
            throw new CustomException("A cancelled appointment cannot be rescheduled.");
        }

        if (original.RescheduledToAppointmentId is not null)
        {
            throw new CustomException("This appointment has already been rescheduled.");
        }

        await AppointmentRefValidator.EnsureRefsExistAsync(
            mediator, original.ClinicId, command.ProviderId, original.AppointmentTypeId, cancellationToken)
            .ConfigureAwait(false);

        Domain.Appointment replacement = Domain.Appointment.Create(
            original.ClinicId, command.ProviderId, original.PatientId, original.AppointmentTypeId,
            command.StartUtc, command.EndUtc, original.Notes);

        await dbContext.Appointments.AddAsync(replacement, cancellationToken).ConfigureAwait(false);
        original.MarkRescheduled(replacement.Id);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await notifier.NotifyChangedAsync(original.ClinicId, original.ProviderId, original.StartUtc, original.EndUtc, "rescheduled", cancellationToken)
            .ConfigureAwait(false);
        await notifier.NotifyChangedAsync(replacement.ClinicId, replacement.ProviderId, replacement.StartUtc, replacement.EndUtc, "created", cancellationToken)
            .ConfigureAwait(false);
        return replacement.Id;
    }
}
