using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;

public sealed record CreatePreferredContactMethodCommand(string Name) : ICommand<int>;
