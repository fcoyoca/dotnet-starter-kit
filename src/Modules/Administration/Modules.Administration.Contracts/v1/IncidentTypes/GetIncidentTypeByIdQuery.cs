using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.IncidentTypes;

public sealed record GetIncidentTypeByIdQuery(Guid Id) : IQuery<IncidentTypeDto>;
