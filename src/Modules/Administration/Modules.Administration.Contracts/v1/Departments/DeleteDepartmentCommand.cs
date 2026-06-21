using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Departments;

public sealed record DeleteDepartmentCommand(Guid Id) : ICommand<Unit>;
