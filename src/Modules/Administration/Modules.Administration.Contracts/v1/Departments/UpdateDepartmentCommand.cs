using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Departments;

public sealed record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    int DisplayOrder,
    bool IsActive) : ICommand<Unit>;
