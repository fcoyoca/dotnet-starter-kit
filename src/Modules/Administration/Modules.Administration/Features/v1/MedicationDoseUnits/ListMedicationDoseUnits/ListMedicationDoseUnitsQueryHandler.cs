using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.MedicationDoseUnits.ListMedicationDoseUnits;

public sealed class ListMedicationDoseUnitsQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<ListMedicationDoseUnitsQuery, IReadOnlyList<MedicationDoseUnitDto>>
{
    public async ValueTask<IReadOnlyList<MedicationDoseUnitDto>> Handle(ListMedicationDoseUnitsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        IQueryable<MedicationDoseUnit> q = dbContext.MedicationDoseUnits.AsNoTracking();
        if (query.IsActive.HasValue)
        {
            q = q.Where(m => m.IsActive == query.IsActive.Value);
        }

        List<MedicationDoseUnit> items = await q.OrderBy(m => m.Name).ToListAsync(cancellationToken).ConfigureAwait(false);
        return items.Select(m => new MedicationDoseUnitDto(m.Id, m.Name, m.IsActive)).ToList();
    }
}
