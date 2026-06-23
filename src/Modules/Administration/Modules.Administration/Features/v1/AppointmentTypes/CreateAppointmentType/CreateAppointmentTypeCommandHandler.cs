using FSH.Modules.Administration.Contracts.v1.AppointmentTypes;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.AppointmentTypes.CreateAppointmentType;

public sealed class CreateAppointmentTypeCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateAppointmentTypeCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateAppointmentTypeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        AppointmentType entity = AppointmentType.Create(
            command.Name, command.Color, command.DefaultDurationMinutes, command.DisplayOrder);
        dbContext.AppointmentTypes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
