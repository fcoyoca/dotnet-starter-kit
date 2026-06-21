using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Departments;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Departments.DeleteDepartment;

public sealed class DeleteDepartmentCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteDepartmentCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteDepartmentCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.Department entity = await dbContext.Departments
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Department {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
