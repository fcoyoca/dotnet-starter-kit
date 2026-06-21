using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Providers;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Providers.CreateProvider;

public sealed class CreateProviderCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateProviderCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProviderCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

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

        Provider entity = Provider.Create(
            command.FirstName,
            command.LastName,
            command.Prefix,
            command.Suffix,
            command.Specialty,
            command.Npi,
            command.KareoExternalId,
            command.PrimaryClinicId,
            command.UserId);
        dbContext.Providers.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
