using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.IncidentTypes;

public sealed record DeleteIncidentTypeCommand(Guid Id) : ICommand<Unit>;
