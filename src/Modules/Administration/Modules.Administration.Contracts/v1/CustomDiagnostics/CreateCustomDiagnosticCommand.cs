using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;

public sealed record CreateCustomDiagnosticCommand(
    string Code,
    string? Description = null,
    string? LongDescription = null,
    bool IsChiropractic = false) : ICommand<Guid>;
