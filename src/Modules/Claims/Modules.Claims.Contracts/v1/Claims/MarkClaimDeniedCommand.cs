using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record MarkClaimDeniedCommand(Guid ClaimId) : ICommand<Guid>;
