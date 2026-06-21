using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.InsuranceCompanies;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.InsuranceCompanies.DeleteInsuranceCompany;

public sealed class DeleteInsuranceCompanyCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteInsuranceCompanyCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteInsuranceCompanyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.InsuranceCompany entity = await dbContext.InsuranceCompanies
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance company {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
