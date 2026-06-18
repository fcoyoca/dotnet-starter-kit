using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Races;

public sealed record UpdateRaceCommand(int Id, string Name, bool IsActive) : ICommand<Unit>;
