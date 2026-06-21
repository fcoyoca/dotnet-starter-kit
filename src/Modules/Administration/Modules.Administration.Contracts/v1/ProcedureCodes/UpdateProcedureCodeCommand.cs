using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

public sealed record UpdateProcedureCodeCommand(
    Guid Id,
    string Code,
    Guid ProcedureCategoryId,
    string? Name,
    string? Description,
    int? CodeSourceId,
    string? MacroText,
    bool IsActive) : ICommand<Unit>;
