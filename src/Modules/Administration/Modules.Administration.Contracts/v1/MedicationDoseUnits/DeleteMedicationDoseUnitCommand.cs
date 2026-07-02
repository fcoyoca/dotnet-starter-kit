using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;

public sealed record DeleteMedicationDoseUnitCommand(int Id) : ICommand<Unit>;
