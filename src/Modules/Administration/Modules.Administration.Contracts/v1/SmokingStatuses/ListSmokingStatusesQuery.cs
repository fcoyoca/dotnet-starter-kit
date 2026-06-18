using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.SmokingStatuses;

public sealed record ListSmokingStatusesQuery(bool? IsActive = null) : IQuery<IReadOnlyList<LookupItemDto>>;
