using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.IncidentTypes;

public sealed record CreateIncidentTypeCommand(string Name) : ICommand<Guid>;
