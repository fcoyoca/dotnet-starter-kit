using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Languages;

public sealed record UpdateLanguageCommand(int Id, string Name, bool IsActive) : ICommand<Unit>;
