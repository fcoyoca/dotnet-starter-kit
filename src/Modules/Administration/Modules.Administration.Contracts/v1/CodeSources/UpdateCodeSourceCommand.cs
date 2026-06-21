using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CodeSources;

public sealed record UpdateCodeSourceCommand(int Id, string Name, bool IsActive) : ICommand<Unit>;
