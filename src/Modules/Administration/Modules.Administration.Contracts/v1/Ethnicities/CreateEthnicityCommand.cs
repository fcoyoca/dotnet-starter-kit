using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Ethnicities;

public sealed record CreateEthnicityCommand(string Name) : ICommand<int>;
