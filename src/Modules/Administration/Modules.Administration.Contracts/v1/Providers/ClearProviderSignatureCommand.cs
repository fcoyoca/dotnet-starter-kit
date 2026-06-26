using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

/// <summary>Remove a provider's signature image (idempotent — no-op when unset).</summary>
public sealed record ClearProviderSignatureCommand(Guid ProviderId) : ICommand<Unit>;
