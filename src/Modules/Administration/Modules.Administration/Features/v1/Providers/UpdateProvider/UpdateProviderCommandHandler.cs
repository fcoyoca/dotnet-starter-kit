using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.UpdateProvider;

public sealed class UpdateProviderCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateProviderCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateProviderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.Provider entity = await dbContext.Providers
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Provider {command.Id} not found.");

        if (command.PrimaryClinicId is { } clinicId)
        {
            bool clinicExists = await dbContext.Clinics
                .AnyAsync(c => c.Id == clinicId, cancellationToken)
                .ConfigureAwait(false);
            if (!clinicExists)
            {
                throw new NotFoundException($"Clinic {clinicId} not found.");
            }
        }

        entity.Update(
            command.FirstName,
            command.LastName,
            command.Prefix,
            command.Suffix,
            command.Specialty,
            command.Npi,
            command.KareoExternalId,
            command.PrimaryClinicId,
            command.UserId,
            command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
