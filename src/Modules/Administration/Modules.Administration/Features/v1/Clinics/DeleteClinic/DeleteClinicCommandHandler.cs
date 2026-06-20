using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Clinics.DeleteClinic;

public sealed class DeleteClinicCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteClinicCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteClinicCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Clinic entity = await dbContext.Clinics
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Clinic {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
