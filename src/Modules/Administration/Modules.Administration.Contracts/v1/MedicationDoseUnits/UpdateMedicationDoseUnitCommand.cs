using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.MedicationDoseUnits;

public sealed record UpdateMedicationDoseUnitCommand(int Id, string Name, bool IsActive) : ICommand<Unit>;
