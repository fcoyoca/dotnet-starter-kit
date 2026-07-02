using FSH.Framework.Core.Exceptions;
using FSH.Modules.Patient.Contracts.Dtos;
using FSH.Modules.Patient.Contracts.v1.PatientAllergies;
using FSH.Modules.Patient.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Patient.Features.v1.PatientAllergies.GetPatientAllergyById;

public sealed class GetPatientAllergyByIdQueryHandler(PatientDbContext dbContext)
    : IQueryHandler<GetPatientAllergyByIdQuery, PatientAllergyDto>
{
    public async ValueTask<PatientAllergyDto> Handle(GetPatientAllergyByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        PatientAllergyDto? dto = await dbContext.PatientAllergies
            .AsNoTracking()
            .Where(a => a.Id == query.AllergyId)
            .Select(a => new PatientAllergyDto(
                a.Id, a.PatientId, a.DrugName, a.RxAui, a.Reaction, a.Comments,
                a.DateNoted, a.IsActive, a.CreatedByName, a.CreatedAtUtc, a.UpdatedByName, a.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return dto ?? throw new NotFoundException($"Allergy {query.AllergyId} not found.");
    }
}
