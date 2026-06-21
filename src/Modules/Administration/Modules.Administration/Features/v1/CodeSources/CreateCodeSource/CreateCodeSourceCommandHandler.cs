using FSH.Modules.Administration.Contracts.v1.CodeSources;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.CodeSources.CreateCodeSource;

public sealed class CreateCodeSourceCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateCodeSourceCommand, int>
{
    public async ValueTask<int> Handle(CreateCodeSourceCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        CodeSource entity = CodeSource.Create(command.Name);
        dbContext.CodeSources.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
