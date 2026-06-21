using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCodes;

public sealed record CreateProcedureCodeCommand(
    string Code,
    string? Name = null,
    string? Description = null,
    Guid? ProcedureCategoryId = null,
    string? CodeSource = null,
    string? MacroText = null) : ICommand<Guid>;
