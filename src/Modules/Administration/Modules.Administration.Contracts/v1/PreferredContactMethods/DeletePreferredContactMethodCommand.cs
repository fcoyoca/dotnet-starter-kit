using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;

public sealed record DeletePreferredContactMethodCommand(int Id) : ICommand<Unit>;
