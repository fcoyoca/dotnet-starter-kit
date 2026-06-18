using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Races;

public sealed record CreateRaceCommand(string Name) : ICommand<int>;
