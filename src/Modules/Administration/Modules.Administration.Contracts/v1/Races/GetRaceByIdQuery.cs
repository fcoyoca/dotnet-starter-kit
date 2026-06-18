using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Races;

public sealed record GetRaceByIdQuery(int Id) : IQuery<LookupItemDto>;
