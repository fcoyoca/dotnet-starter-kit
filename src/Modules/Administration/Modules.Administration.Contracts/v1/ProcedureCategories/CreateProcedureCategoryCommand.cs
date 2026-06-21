using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

public sealed record CreateProcedureCategoryCommand(
    string Name,
    string? Description = null,
    bool IsImaging = false) : ICommand<Guid>;
