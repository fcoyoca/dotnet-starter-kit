using FSH.Framework.Core.Exceptions;
using FSH.Modules.Administration.Contracts.v1.Languages;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Administration.Features.v1.Languages.DeleteLanguage;

public sealed class DeleteLanguageCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<DeleteLanguageCommand, Unit>
{
    public async ValueTask<Unit> Handle(DeleteLanguageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Language entity = await dbContext.Languages
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Language {command.Id} not found.");
        entity.Delete(deletedBy: null);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
