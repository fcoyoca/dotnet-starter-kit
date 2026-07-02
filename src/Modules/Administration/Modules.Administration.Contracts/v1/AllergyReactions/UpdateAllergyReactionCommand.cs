using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AllergyReactions;

public sealed record UpdateAllergyReactionCommand(int Id, string Term, string? SnomedCode, bool IsActive) : ICommand<Unit>;
