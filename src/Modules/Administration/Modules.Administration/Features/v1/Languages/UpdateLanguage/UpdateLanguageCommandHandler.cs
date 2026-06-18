using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Languages;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Languages.UpdateLanguage;

public sealed class UpdateLanguageCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<UpdateLanguageCommand, Unit>
{
    public async ValueTask<Unit> Handle(UpdateLanguageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Language entity = await dbContext.Languages
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Language {command.Id} not found.");
        entity.Update(command.Name, command.IsActive);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
