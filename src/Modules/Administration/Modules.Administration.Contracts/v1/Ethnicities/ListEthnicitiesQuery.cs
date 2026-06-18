using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Ethnicities;

public sealed record ListEthnicitiesQuery(bool? IsActive = null) : IQuery<IReadOnlyList<LookupItemDto>>;
