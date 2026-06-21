using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.UpdateCustomDiagnostic;

public sealed class UpdateCustomDiagnosticCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateCustomDiagnosticCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateCustomDiagnosticCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        Domain.CustomDiagnostic entity = await dbContext.CustomDiagnostics
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Custom diagnostic {command.Id} not found.");

        entity.Update(
            command.Code,
            command.Description,
            command.LongDescription,
            command.IsChiropractic,
            command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
