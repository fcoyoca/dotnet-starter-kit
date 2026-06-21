using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.UpdateDiagnosticCategory;

public sealed class UpdateDiagnosticCategoryCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateDiagnosticCategoryCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateDiagnosticCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.DiagnosticCategory entity = await dbContext.DiagnosticCategories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Diagnostic category {command.Id} not found.");

        entity.Update(command.Name, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
