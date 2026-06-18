using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Races;

public sealed record DeleteRaceCommand(int Id) : ICommand<Unit>;
