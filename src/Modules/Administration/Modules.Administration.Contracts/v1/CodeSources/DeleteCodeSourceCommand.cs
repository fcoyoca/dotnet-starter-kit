using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CodeSources;

public sealed record DeleteCodeSourceCommand(int Id) : ICommand<Unit>;
