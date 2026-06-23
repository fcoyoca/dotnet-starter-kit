using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Macros;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Macros.CreateMacro;

public sealed class CreateMacroCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateMacroCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateMacroCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ReportFieldId is { } reportFieldId)
        {
            bool fieldExists = await dbContext.ReportFields
                .AnyAsync(f => f.Id == reportFieldId, cancellationToken)
                .ConfigureAwait(false);
            if (!fieldExists)
            {
                throw new NotFoundException($"Report field {reportFieldId} not found.");
            }
        }

        Macro entity = Macro.Create(command.Name, command.Text, command.ReportFieldId, command.UseableByUserId);
        dbContext.Macros.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
