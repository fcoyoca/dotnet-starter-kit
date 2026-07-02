using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AllergyReactions;

public sealed record DeleteAllergyReactionCommand(int Id) : ICommand<Unit>;
