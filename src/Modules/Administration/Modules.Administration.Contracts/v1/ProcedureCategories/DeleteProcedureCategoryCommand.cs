using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.ProcedureCategories;

public sealed record DeleteProcedureCategoryCommand(Guid Id) : ICommand<Unit>;
