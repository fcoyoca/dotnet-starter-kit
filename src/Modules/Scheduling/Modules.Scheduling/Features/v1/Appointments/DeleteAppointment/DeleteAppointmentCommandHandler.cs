using FSH.Framework.Core.Exceptions;
using FSH.Modules.Scheduling.Contracts.v1.Appointments;
using FSH.Modules.Scheduling.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Scheduling.Features.v1.Appointments.DeleteAppointment;

public sealed class DeleteAppointmentCommandHandler(SchedulingDbContext dbContext)
    : ICommandHandler<DeleteAppointmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteAppointmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Appointment entity = await dbContext.Appointments
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment {command.Id} not found.");

        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
