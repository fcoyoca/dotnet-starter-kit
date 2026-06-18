using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Ethnicities;

public sealed record DeleteEthnicityCommand(int Id) : ICommand<Unit>;
