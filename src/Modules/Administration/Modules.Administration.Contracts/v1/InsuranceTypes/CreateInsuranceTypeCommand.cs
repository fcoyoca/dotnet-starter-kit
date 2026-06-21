using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

public sealed record CreateInsuranceTypeCommand(string Name) : ICommand<Guid>;
