using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Macros;

public sealed record UpdateMacroCommand(
    Guid Id,
    string Name,
    string? Text,
    int? ReportFieldId,
    Guid? UseableByUserId,
    bool IsActive) : ICommand<Unit>;
