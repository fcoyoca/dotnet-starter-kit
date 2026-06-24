using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Features.v1.Appointments;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.UpdateAppointment;

public sealed class UpdateAppointmentCommandHandler(SchedulingDbContext dbContext, IMediator mediator)
    : ICommandHandler<UpdateAppointmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Appointment entity = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment {command.Id} not found.");

        await AppointmentRefValidator.EnsureRefsExistAsync(
            mediator, command.ClinicId, command.ProviderId, command.AppointmentTypeId, cancellationToken)
            .ConfigureAwait(false);

        entity.Update(command.ClinicId, command.ProviderId, command.PatientId, command.AppointmentTypeId,
            command.StartUtc, command.EndUtc, command.Notes);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
