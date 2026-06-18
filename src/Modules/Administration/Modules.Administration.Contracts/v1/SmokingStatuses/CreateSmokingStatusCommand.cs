using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

public sealed record CreateSmokingStatusCommand(string Name, string? SnomedCode = null) : ICommand<int>;
