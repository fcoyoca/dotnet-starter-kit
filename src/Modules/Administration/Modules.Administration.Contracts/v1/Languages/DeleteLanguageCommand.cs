using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Languages;

public sealed record DeleteLanguageCommand(int Id) : ICommand<Unit>;
