using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;

public sealed record CreateMedicationDoseUnitCommand(string Name) : ICommand<int>;
