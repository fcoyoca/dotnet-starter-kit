using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Diagnostics;

public sealed record CreateDiagnosticCommand(
    string Code,
    string? Description = null,
    string? LongDescription = null,
    int CodeSourceId = 7,
    bool IsChiropractic = false,
    bool? IsBillable = null) : ICommand<int>;
