using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

public sealed record UpdateDiagnosticCategoryCommand(
    Guid Id,
    string Name,
    bool IsActive) : ICommand<Unit>;
