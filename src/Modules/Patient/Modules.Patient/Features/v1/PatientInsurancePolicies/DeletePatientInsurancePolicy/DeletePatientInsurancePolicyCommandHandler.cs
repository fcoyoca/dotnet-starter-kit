using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.DeletePatientInsurancePolicy;

public sealed class DeletePatientInsurancePolicyCommandHandler(PatientDbContext dbContext)
    : ICommandHandler<DeletePatientInsurancePolicyCommand, Unit>
{
    public async ValueTask<Unit> Handle(
        DeletePatientInsurancePolicyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.PatientInsurancePolicy policy = await dbContext.PatientInsurancePolicies
            .FirstOrDefaultAsync(x => x.Id == command.PolicyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance policy {command.PolicyId} not found.");

        dbContext.PatientInsurancePolicies.Remove(policy);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
