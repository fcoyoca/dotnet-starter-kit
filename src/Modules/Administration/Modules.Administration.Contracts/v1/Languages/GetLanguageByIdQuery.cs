using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Languages;

public sealed record GetLanguageByIdQuery(int Id) : IQuery<LookupItemDto>;
