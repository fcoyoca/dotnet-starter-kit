using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.InsuranceTypes;

public sealed record DeleteInsuranceTypeCommand(Guid Id) : ICommand<Unit>;
