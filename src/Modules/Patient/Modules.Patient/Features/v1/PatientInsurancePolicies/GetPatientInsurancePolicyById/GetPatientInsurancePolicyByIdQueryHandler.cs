using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientInsurancePolicies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientInsurancePolicies.GetPatientInsurancePolicyById;

public sealed class GetPatientInsurancePolicyByIdQueryHandler(PatientDbContext dbContext, IMediator mediator)
    : IQueryHandler<GetPatientInsurancePolicyByIdQuery, PatientInsurancePolicyDto>
{
    public async ValueTask<PatientInsurancePolicyDto> Handle(
        GetPatientInsurancePolicyByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Domain.PatientInsurancePolicy policy = await dbContext.PatientInsurancePolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == query.PolicyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Insurance policy {query.PolicyId} not found.");

        (Dictionary<Guid, string> companies, Dictionary<Guid, string> types) =
            await InsuranceLookupNames.ResolveAsync(mediator, cancellationToken).ConfigureAwait(false);

        return policy.ToDto(
            companies.NameOrNull(policy.InsuranceCompanyId),
            types.NameOrNull(policy.InsuranceTypeId));
    }
}
