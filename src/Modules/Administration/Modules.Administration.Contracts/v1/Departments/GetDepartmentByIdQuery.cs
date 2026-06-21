using FSH.Modules.Administration.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Administration.Contracts.v1.Departments;

public sealed record GetDepartmentByIdQuery(Guid Id) : IQuery<DepartmentDto>;
