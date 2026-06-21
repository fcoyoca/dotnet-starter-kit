using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CodeSources;

public sealed record GetCodeSourceByIdQuery(int Id) : IQuery<LookupItemDto>;
