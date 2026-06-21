using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.DeleteDiagnosticCategory;

public sealed class DeleteDiagnosticCategoryCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteDiagnosticCategoryCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteDiagnosticCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.DiagnosticCategory entity = await dbContext.DiagnosticCategories
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Diagnostic category {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
