using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record MarkClaimReadyCommand(Guid ClaimId) : ICommand<Guid>;
