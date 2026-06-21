using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

public sealed record CreateProcedureCodeCommand(
    string Code,
    Guid ProcedureCategoryId,
    string? Name = null,
    string? Description = null,
    string? CodeSource = null,
    string? MacroText = null) : ICommand<Guid>;
