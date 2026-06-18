using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

public sealed record GetSmokingStatusByIdQuery(int Id) : IQuery<LookupItemDto>;
