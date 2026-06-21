using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Departments;

public sealed record CreateDepartmentCommand(string Name, int DisplayOrder = 0) : ICommand<Guid>;
