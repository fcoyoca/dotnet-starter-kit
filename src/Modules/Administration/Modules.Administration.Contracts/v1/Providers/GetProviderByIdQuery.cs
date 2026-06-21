using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Providers;

public sealed record GetProviderByIdQuery(Guid Id) : IQuery<ProviderDto>;
