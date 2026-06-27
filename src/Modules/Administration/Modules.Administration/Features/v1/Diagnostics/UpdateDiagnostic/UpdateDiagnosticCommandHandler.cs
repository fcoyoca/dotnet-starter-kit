using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.UpdateDiagnostic;

public sealed class UpdateDiagnosticCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateDiagnosticCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateDiagnosticCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Diagnostic entity = await dbContext.Diagnostics
            .FirstOrDefaultAsync(d => d.Id == command.Id && !d.IsDeleted, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Diagnostic {command.Id} not found.");

        entity.Update(
            command.Code,
            command.Description,
            command.LongDescription,
            command.CodeSourceId,
            command.IsChiropractic,
            command.IsBillable,
            command.IsActive);

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
