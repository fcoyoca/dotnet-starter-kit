using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Macros;

public sealed record UpdateMacroCommand(
    Guid Id,
    string Name,
    string? Text,
    int? ReportFieldId,
    bool IsActive) : ICommand<Unit>;
