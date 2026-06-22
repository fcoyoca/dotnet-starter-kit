using FSH.Modules.Administration.Contracts.v1.Macros;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Macros.CreateMacro;

public sealed class CreateMacroCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateMacroCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateMacroCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Macro entity = Macro.Create(command.Name, command.Text);
        dbContext.Macros.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
