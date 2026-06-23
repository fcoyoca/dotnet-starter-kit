using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.UpdateAppointmentType;

public sealed class UpdateAppointmentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateAppointmentTypeCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateAppointmentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.AppointmentType entity = await dbContext.AppointmentTypes
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Appointment type {command.Id} not found.");

        entity.Update(command.Name, command.Color, command.DefaultDurationMinutes, command.DisplayOrder, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
