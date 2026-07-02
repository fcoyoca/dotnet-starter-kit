using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.AllergyReactions;

public sealed record GetAllergyReactionByIdQuery(int Id) : IQuery<AllergyReactionDto>;
