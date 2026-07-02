using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AllergyReactions;

public sealed record CreateAllergyReactionCommand(string Term, string? SnomedCode = null) : ICommand<int>;
