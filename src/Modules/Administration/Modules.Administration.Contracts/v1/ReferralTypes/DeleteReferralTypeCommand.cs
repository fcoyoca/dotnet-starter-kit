using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReferralTypes;

public sealed record DeleteReferralTypeCommand(int Id) : ICommand<Unit>;
