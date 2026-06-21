using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Departments.UpdateDepartment;

public sealed class UpdateDepartmentCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateDepartmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateDepartmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Department entity = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Department {command.Id} not found.");
        entity.Update(command.Name, command.DisplayOrder, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
