using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;

public sealed record UpdatePreferredContactMethodCommand(int Id, string Name, bool IsActive) : ICommand<Unit>;
