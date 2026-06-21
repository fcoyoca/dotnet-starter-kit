using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

public sealed record UpdateCustomDiagnosticCommand(
    Guid Id,
    string Code,
    string? Description,
    string? LongDescription,
    bool IsChiropractic,
    bool IsActive) : ICommand<Unit>;
