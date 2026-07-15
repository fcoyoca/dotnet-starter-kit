using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record VoidClaimCommand(Guid ClaimId, string? Reason = null) : ICommand<Guid>;
