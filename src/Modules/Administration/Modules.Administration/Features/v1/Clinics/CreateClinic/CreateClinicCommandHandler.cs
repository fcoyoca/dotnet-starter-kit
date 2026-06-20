using FSH.Modules.Administration.Contracts.v1.Clinics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Clinics.CreateClinic;

public sealed class CreateClinicCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateClinicCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateClinicCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Clinic entity = Clinic.Create(
            command.Code,
            command.Name,
            command.Address1,
            command.Address2,
            command.City,
            command.State,
            command.Zip,
            command.Phone);
        dbContext.Clinics.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
