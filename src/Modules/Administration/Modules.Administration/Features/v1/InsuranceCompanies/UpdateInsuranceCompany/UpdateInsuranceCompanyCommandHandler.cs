using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.UpdateInsuranceCompany;

public sealed class UpdateInsuranceCompanyCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateInsuranceCompanyCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateInsuranceCompanyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.InsuranceCompany entity = await dbContext.InsuranceCompanies
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance company {command.Id} not found.");

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

        entity.Update(
            command.Name,
            command.InsuranceTypeId,
            command.FormularyTiers,
            command.Address1,
            command.Address2,
            command.City,
            command.State,
            command.Zip,
            command.Phone,
            command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
