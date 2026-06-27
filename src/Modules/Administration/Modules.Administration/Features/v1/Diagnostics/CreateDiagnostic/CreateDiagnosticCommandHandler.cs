using FSH.Modules.Administration.Contracts.v1.Diagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.Diagnostics.CreateDiagnostic;

public sealed class CreateDiagnosticCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateDiagnosticCommand, int>
{
    public async ValueTask<int> Handle(CreateDiagnosticCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        Diagnostic entity = Diagnostic.Create(
            command.Code,
            command.Description,
            command.LongDescription,
            command.CodeSourceId,
            command.IsChiropractic,
            command.IsBillable);
        dbContext.Diagnostics.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
