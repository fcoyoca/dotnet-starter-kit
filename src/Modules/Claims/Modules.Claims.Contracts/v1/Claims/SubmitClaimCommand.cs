using Mediator;

namespace FSH.Modules.Claims.Contracts.v1.Claims;

public sealed record SubmitClaimCommand(Guid ClaimId) : ICommand<Guid>;
