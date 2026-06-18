using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Ethnicities;

public sealed record UpdateEthnicityCommand(int Id, string Name, bool IsActive) : ICommand<Unit>;
