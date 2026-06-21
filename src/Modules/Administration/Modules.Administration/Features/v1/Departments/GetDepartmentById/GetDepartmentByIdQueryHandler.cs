using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.Dtos;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Departments.GetDepartmentById;

public sealed class GetDepartmentByIdQueryHandler(AdministrationDbContext dbContext)
    : IQueryHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    public async ValueTask<DepartmentDto> Handle(GetDepartmentByIdQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        Domain.Department entity = await dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == query.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Department {query.Id} not found.");
        return new DepartmentDto(
            entity.Id, entity.Name, entity.DisplayOrder, entity.IsActive,
            entity.CreatedAtUtc, entity.UpdatedAtUtc);
    }
}
