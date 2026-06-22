using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.IncidentTypes;

public sealed record UpdateIncidentTypeCommand(
    Guid Id,
    string Name,
    bool IsActive) : ICommand<Unit>;
