using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using Mediator;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.CreateAppointment;

public sealed class CreateAppointmentCommandHandler(
    SchedulingDbContext dbContext, IMediator mediator, AppointmentRealtimeNotifier notifier)
    : ICommandHandler<CreateAppointmentCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        await AppointmentRefValidator.EnsureRefsExistAsync(
            mediator, command.ClinicId, command.ProviderId, command.AppointmentTypeId, cancellationToken)
            .ConfigureAwait(false);

        Domain.Appointment entity = Domain.Appointment.Create(
            command.ClinicId, command.ProviderId, command.PatientId, command.AppointmentTypeId,
            command.StartUtc, command.EndUtc, command.Notes);
        await dbContext.Appointments.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await notifier.NotifyChangedAsync(entity.ClinicId, entity.ProviderId, entity.StartUtc, entity.EndUtc, "created", cancellationToken)
            .ConfigureAwait(false);
        return entity.Id;
    }
}
