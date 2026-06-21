using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

public sealed record DeleteDiagnosticCategoryCommand(Guid Id) : ICommand<Unit>;
