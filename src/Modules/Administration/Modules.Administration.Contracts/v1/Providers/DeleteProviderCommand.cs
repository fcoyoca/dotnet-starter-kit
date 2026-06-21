using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

public sealed record DeleteProviderCommand(Guid Id) : ICommand<Unit>;
