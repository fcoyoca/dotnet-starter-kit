using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.CreateInsuranceCompany;

public sealed class CreateInsuranceCompanyCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateInsuranceCompanyCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateInsuranceCompanyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.InsuranceTypeId is { } typeId)
        {
            bool typeExists = await dbContext.InsuranceTypes
                .AnyAsync(t => t.Id == typeId, cancellationToken)
                .ConfigureAwait(false);
            if (!typeExists)
            {
                throw new NotFoundException($"Insurance type {typeId} not found.");
            }
        }

        InsuranceCompany entity = InsuranceCompany.Create(
            command.Name,
            command.InsuranceTypeId,
            command.FormularyTiers,
            command.Address1,
            command.Address2,
            command.City,
            command.State,
            command.Zip,
            command.Phone);
        dbContext.InsuranceCompanies.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
