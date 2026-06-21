using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

public sealed record UpdateProcedureCodeCommand(
    Guid Id,
    string Code,
    string? Name,
    string? Description,
    Guid? ProcedureCategoryId,
    string? CodeSource,
    string? MacroText,
    bool IsActive) : ICommand<Unit>;
