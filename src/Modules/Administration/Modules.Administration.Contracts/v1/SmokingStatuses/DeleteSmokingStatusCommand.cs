using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

public sealed record DeleteSmokingStatusCommand(int Id) : ICommand<Unit>;
