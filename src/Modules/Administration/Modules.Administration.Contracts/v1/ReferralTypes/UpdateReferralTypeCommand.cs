using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReferralTypes;

public sealed record UpdateReferralTypeCommand(int Id, string Name, bool IsActive) : ICommand<Unit>;
