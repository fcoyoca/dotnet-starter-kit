using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.DeleteAppointmentType;

public sealed class DeleteAppointmentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteAppointmentTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteAppointmentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.AppointmentType entity = await dbContext.AppointmentTypes
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment type {command.Id} not found.");

        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
