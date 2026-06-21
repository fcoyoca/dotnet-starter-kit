using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;

public sealed record CreateDiagnosticCategoryCommand(string Name) : ICommand<Guid>;
