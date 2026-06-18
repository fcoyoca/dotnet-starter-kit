using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Languages;

public sealed record CreateLanguageCommand(string Name) : ICommand<int>;
