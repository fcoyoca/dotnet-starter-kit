using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

public sealed record UpdateInsuranceTypeCommand(Guid Id, string Name, bool IsActive) : ICommand<Unit>;
