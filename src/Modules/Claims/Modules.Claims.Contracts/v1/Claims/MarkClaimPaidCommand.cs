using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record MarkClaimPaidCommand(Guid ClaimId) : ICommand<Guid>;
