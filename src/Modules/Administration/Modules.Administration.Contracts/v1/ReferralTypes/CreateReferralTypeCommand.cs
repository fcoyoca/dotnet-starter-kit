using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ReferralTypes;

public sealed record CreateReferralTypeCommand(string Name) : ICommand<int>;
