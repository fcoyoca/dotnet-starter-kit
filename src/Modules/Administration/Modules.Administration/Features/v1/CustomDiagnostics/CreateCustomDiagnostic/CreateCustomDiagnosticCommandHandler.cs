using FSH.Modules.Administration.Contracts.v1.CustomDiagnostics;
using FSH.Modules.Administration.Data;
using FSH.Modules.Administration.Domain;
using Mediator;

namespace FSH.Modules.Administration.Features.v1.CustomDiagnostics.CreateCustomDiagnostic;

public sealed class CreateCustomDiagnosticCommandHandler(AdministrationDbContext dbContext)
    : ICommandHandler<CreateCustomDiagnosticCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateCustomDiagnosticCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        CustomDiagnostic entity = CustomDiagnostic.Create(
            command.Code,
            command.Description,
            command.LongDescription,
            command.IsChiropractic);
        dbContext.CustomDiagnostics.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}
