using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;

public sealed record GetMedicationDoseUnitByIdQuery(int Id) : IQuery<MedicationDoseUnitDto>;
