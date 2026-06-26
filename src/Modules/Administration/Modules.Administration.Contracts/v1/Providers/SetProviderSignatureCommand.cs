using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

/// <summary>
/// Upload (or replace) a provider's signature image. <see cref="ImageBase64"/> is a base64 PNG,
/// optionally with a <c>data:image/png;base64,</c> prefix. Returns the stored image's public URL.
/// </summary>
public sealed record SetProviderSignatureCommand(Guid ProviderId, string ImageBase64) : ICommand<string>;
