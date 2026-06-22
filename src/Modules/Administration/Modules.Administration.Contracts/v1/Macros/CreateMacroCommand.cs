using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Macros;

public sealed record CreateMacroCommand(string Name, string? Text = null) : ICommand<Guid>;
