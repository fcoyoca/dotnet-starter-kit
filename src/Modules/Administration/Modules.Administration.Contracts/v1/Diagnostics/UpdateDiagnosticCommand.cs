using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Diagnostics;

public sealed record UpdateDiagnosticCommand(
    int Id,
    string Code,
    string? Description,
    string? LongDescription,
    int CodeSourceId,
    bool IsChiropractic,
    bool? IsBillable,
    bool IsActive) : ICommand<Unit>;
