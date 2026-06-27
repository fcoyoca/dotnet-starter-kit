using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.DeleteDiagnostic;

public sealed class DeleteDiagnosticCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteDiagnosticCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteDiagnosticCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Diagnostic entity = await dbContext.Diagnostics
            .FirstOrDefaultAsync(d => d.Id == command.Id && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Diagnostic {command.Id} not found.");

        entity.Delete(null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
