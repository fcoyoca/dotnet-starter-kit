using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.DeleteCustomDiagnostic;

public sealed class DeleteCustomDiagnosticCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteCustomDiagnosticCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteCustomDiagnosticCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Domain.CustomDiagnostic entity = await dbContext.CustomDiagnostics
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Custom diagnostic {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
