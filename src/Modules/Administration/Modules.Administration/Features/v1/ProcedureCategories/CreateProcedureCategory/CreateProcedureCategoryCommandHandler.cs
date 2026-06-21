using FSH.Modules.Administration.Contracts.v1.ProcedureCategories;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.ProcedureCategories.CreateProcedureCategory;

public sealed class CreateProcedureCategoryCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateProcedureCategoryCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateProcedureCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ProcedureCategory entity = ProcedureCategory.Create(command.Name, command.Description, command.IsImaging);
        dbContext.ProcedureCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
