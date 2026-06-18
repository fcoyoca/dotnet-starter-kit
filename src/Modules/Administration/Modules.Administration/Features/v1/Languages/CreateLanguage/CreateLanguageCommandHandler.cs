using FSH.Modules.Administration.Contracts.v1.Languages;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Languages.CreateLanguage;

public sealed class CreateLanguageCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateLanguageCommand, int>
{
    public async ValueTask<int> Handle(CreateLanguageCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Language entity = Language.Create(command.Name);
        dbContext.Languages.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
