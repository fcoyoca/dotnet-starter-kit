using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

public sealed record UpdateProcedureCategoryCommand(
    Guid Id,
    string Name,
    string? Description,
    bool IsImaging,
    bool IsActive) : ICommand<Unit>;
