using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

public sealed record EnsureCustomDiagnosticCommand(
    string Code,
    string? Description = null,
    string? LongDescription = null,
    bool IsChiropractic = false) : ICommand<Guid>;
