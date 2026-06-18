using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

public sealed record UpdateSmokingStatusCommand(int Id, string Name, bool IsActive, string? SnomedCode = null) : ICommand<Unit>;
