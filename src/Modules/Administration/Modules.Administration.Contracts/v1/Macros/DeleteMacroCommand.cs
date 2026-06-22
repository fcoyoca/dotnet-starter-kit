using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Macros;

public sealed record DeleteMacroCommand(Guid Id) : ICommand<Unit>;
