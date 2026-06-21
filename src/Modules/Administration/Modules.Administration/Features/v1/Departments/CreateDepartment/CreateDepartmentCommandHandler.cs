using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Departments.CreateDepartment;

public sealed class CreateDepartmentCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateDepartmentCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateDepartmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Department entity = Department.Create(command.Name, command.DisplayOrder);
        dbContext.Departments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
