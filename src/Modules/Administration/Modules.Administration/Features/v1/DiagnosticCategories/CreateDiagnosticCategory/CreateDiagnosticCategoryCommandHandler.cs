using FSH.Modules.Administration.Contracts.v1.DiagnosticCategories;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.DiagnosticCategories.CreateDiagnosticCategory;

public sealed class CreateDiagnosticCategoryCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateDiagnosticCategoryCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateDiagnosticCategoryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        DiagnosticCategory entity = DiagnosticCategory.Create(command.Name);
        dbContext.DiagnosticCategories.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
