using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.PreferredContactMethods;

public sealed record ListPreferredContactMethodsQuery(bool? IsActive = null) : IQuery<IReadOnlyList<LookupItemDto>>;
