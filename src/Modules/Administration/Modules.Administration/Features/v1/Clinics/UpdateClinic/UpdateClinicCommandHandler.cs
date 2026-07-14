using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Clinics.UpdateClinic;

public sealed class UpdateClinicCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateClinicCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateClinicCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Clinic entity = await dbContext.Clinics
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Clinic {command.Id} not found.");
        entity.Update(
            command.Code,
            command.Name,
            command.Address1,
            command.Address2,
            command.City,
            command.State,
            command.Zip,
            command.Phone,
            command.IsActive,
            command.TimeZoneId,
            command.PrintOrientation);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
